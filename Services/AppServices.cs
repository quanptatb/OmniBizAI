using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Domain.StateMachines;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string UserFullName { get; }
    string TenantName { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool HasRole(string role);
}

public class TenantContextService : ITenantContext
{
    private static readonly Guid DemoTenantId = new("00000000-0000-0000-0000-000000000001");
    public Guid TenantId => DemoTenantId;
    public Guid UserId { get; }
    public string UserFullName { get; }
    public string TenantName { get; } = "OmniBiz Demo Company";
    public IReadOnlyCollection<string> Roles { get; }

    public TenantContextService(IHttpContextAccessor http, ApplicationDbContext db)
    {
        var user = http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var email = user.Identity.Name ?? "";
            var appUser = db.AppUsers.AsNoTracking().FirstOrDefault(u => u.Email == email && u.TenantId == DemoTenantId);
            UserId = appUser?.Id ?? Guid.Empty;
            UserFullName = appUser?.FullName ?? email;
            Roles = user.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value).ToList().AsReadOnly();
        }
        else { UserId = Guid.Empty; UserFullName = "Guest"; Roles = Array.Empty<string>(); }
    }

    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

// ─── Dashboard ──────────────────────────────────────────────────────────────
public class DashboardService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var tid = tenant.TenantId;
        var requests = await db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted).ToListAsync();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);

        var vm = new DashboardViewModel
        {
            UserFullName = tenant.UserFullName,
            UserRole = tenant.Roles.FirstOrDefault() ?? "",
            TenantName = tenant.TenantName,
            TotalOperationRequests = requests.Count,
            OverdueTasks = requests.Count(r => IsRequestOverdue(r, now, today)),
            PendingApprovals = await db.ApprovalTasks.CountAsync(t => t.TenantId == tid && t.Status == ApprovalStatus.Pending && !t.IsDeleted),
            ActiveUsers = await db.AppUsers.CountAsync(u => u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted),
            RequestsByStatus = requests.GroupBy(r => r.Status.ToString()).Select(g => new StatusCountItem { Status = g.Key, Count = g.Count() }).ToList(),
            DeptWorkload = await db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted)
                .Join(db.OrganizationUnits, r => r.OrganizationUnitId, o => o.Id, (r, o) => o.Name)
                .GroupBy(n => n).Select(g => new DeptWorkloadItem { Dept = g.Key, Count = g.Count() }).ToListAsync(),
            MonthlyTrend = Enumerable.Range(-5, 6).Select(i => DateTime.Today.AddMonths(i))
                .Select(m => new MonthlyTrendItem
                {
                    Month = m.ToString("MM/yyyy"),
                    Created = requests.Count(r => r.CreatedAt.Year == m.Year && r.CreatedAt.Month == m.Month),
                    Completed = requests.Count(r => r.Status == OperationStatus.Completed && r.UpdatedAt?.Year == m.Year && r.UpdatedAt?.Month == m.Month)
                }).ToList(),
        };

        vm.RecentRequests = await db.OperationRequests
            .Where(r => r.TenantId == tid && !r.IsDeleted).OrderByDescending(r => r.CreatedAt).Take(5)
            .Join(db.AppUsers, r => r.RequestedByUserId, u => u.Id, (r, u) => new RecentRequestItem
            { Id = r.Id, RequestNo = r.RequestNo, Title = r.Title, Type = r.Type, Status = r.Status.ToString(), Priority = r.Priority.ToString(), CreatedBy = u.FullName, CreatedAt = r.CreatedAt, DueDate = r.DueDate })
            .ToListAsync();

        vm.RecentAudits = await db.AuditLogs.Where(a => a.TenantId == tid).OrderByDescending(a => a.CreatedAt).Take(8)
            .Join(db.AppUsers.Where(u => u.TenantId == tid), a => a.UserId, u => u.Id, (a, u) => new RecentAuditItem
            { Action = a.Action, UserName = u.FullName, EntityType = a.EntityName, OccurredAt = a.CreatedAt })
            .ToListAsync();

        vm.KpiSummaries = await db.KpiDefinitions.Where(k => k.TenantId == tid && k.IsActive && !k.IsDeleted).Take(4)
            .Select(k => new KpiSummaryItem { Code = k.Code, Name = k.Name, Unit = k.Unit, Target = 100, Actual = null }).ToListAsync();

        var budgets = await db.Budgets.Where(b => b.TenantId == tid && b.Status == BudgetStatus.Active && !b.IsDeleted).ToListAsync();
        vm.TotalBudget = budgets.Sum(b => b.PlannedAmount);
        vm.UsedBudget = budgets.Sum(b => b.Expenses.Sum(e => e.Amount));

        return vm;
    }

    private static bool IsRequestOverdue(OperationRequest request, DateTimeOffset now, DateOnly today)
    {
        var slaDueAt = OperationSlaService.GetActiveDueAt(request.Status, request.ApprovalDueAt, request.ResolutionDueAt);
        if (slaDueAt.HasValue) return slaDueAt.Value < now;

        return request.DueDate.HasValue
            && request.DueDate.Value < today
            && request.Status != OperationStatus.Completed
            && request.Status != OperationStatus.Cancelled;
    }
}

// ─── OperationRequest ────────────────────────────────────────────────────────
public class OperationRequestService(
    ApplicationDbContext db,
    ITenantContext tenant,
    INumberingService numbering,
    IAuditService audit,
    IOperationSlaService operationSla,
    IOperationSlaWatcherQueue slaWatcherQueue,
    OperationApprovalService operationApprovals,
    IReportCacheService cache)
{
    private const string CriticalOverdueFilter = "CriticalOverdue";
    private const string OverBudgetFilter = "OverBudget";
    private const decimal CostOverrunThresholdPercent = 20m;
    private static readonly Regex MentionRegex = new(@"@([A-Za-z0-9._-]{2,80})", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions TemplateJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    private sealed record OperationAssignmentAccess(bool HasAssignments, bool HasPrimary, bool HasSupport, bool HasWatcher);

    private bool IsOperationAdmin() =>
        tenant.HasRole(OperationRoles.SystemAdmin) || tenant.HasRole(OperationRoles.TenantAdmin);

    private bool HasLegacyOperationManagerRole() =>
        IsOperationAdmin() || tenant.HasRole(OperationRoles.DepartmentManager);

    private bool HasLegacyOperationContributorRole() =>
        HasLegacyOperationManagerRole() || tenant.HasRole(OperationRoles.Staff);

    private bool CanManageOperationAssignments() =>
        IsOperationAdmin() || tenant.HasRole(OperationRoles.DepartmentManager);

    private async Task<List<Guid>> GetCurrentUserDepartmentIdsAsync()
    {
        var tid = tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var departmentIds = await db.EmployeeDepartmentAssignments
            .Where(a => a.TenantId == tid
                && !a.IsDeleted
                && a.EmployeeProfile != null
                && a.EmployeeProfile.UserId == tenant.UserId
                && a.EffectiveFrom <= today
                && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value >= today))
            .Select(a => a.OrganizationUnitId)
            .ToListAsync();

        var primaryDepartmentId = await db.AppUsers
            .AsNoTracking()
            .Where(u => u.Id == tenant.UserId && u.TenantId == tid && !u.IsDeleted)
            .Select(u => u.OrganizationUnitId)
            .FirstOrDefaultAsync();

        if (primaryDepartmentId.HasValue) departmentIds.Add(primaryDepartmentId.Value);
        return departmentIds.Distinct().ToList();
    }

    private async Task<OperationAssignmentAccess> GetAssignmentAccessAsync(Guid requestId)
    {
        var activeAssignments = await db.OperationRequestAssignments
            .AsNoTracking()
            .Where(a => a.TenantId == tenant.TenantId
                && a.OperationRequestId == requestId
                && a.IsActive
                && !a.IsDeleted)
            .Select(a => new { a.Role, a.AssignedUserId, a.OrganizationUnitId })
            .ToListAsync();

        if (!activeAssignments.Any()) return new(false, false, false, false);

        var departmentIds = await GetCurrentUserDepartmentIdsAsync();
        var matchedRoles = activeAssignments
            .Where(a => a.AssignedUserId == tenant.UserId
                || (a.OrganizationUnitId.HasValue && departmentIds.Contains(a.OrganizationUnitId.Value)))
            .Select(a => a.Role)
            .Distinct()
            .ToList();

        return new(
            true,
            matchedRoles.Contains(OperationAssignmentRole.Primary),
            matchedRoles.Contains(OperationAssignmentRole.Support),
            matchedRoles.Contains(OperationAssignmentRole.Watcher));
    }

    private async Task<bool> CanManageRequestWorkAsync(Guid requestId)
    {
        if (IsOperationAdmin()) return true;
        var access = await GetAssignmentAccessAsync(requestId);
        return access.HasAssignments ? access.HasPrimary : HasLegacyOperationManagerRole();
    }

    private async Task<bool> CanSupportRequestAsync(Guid requestId)
    {
        if (IsOperationAdmin()) return true;
        var access = await GetAssignmentAccessAsync(requestId);
        return access.HasAssignments
            ? access.HasPrimary || access.HasSupport
            : HasLegacyOperationContributorRole();
    }

    public Task<bool> CanSupportOperationRequestAsync(Guid requestId) =>
        CanSupportRequestAsync(requestId);

    private async Task<bool> CanSubmitRequestAsync(OperationRequest request)
    {
        if (IsOperationAdmin() || request.RequestedByUserId == tenant.UserId) return true;
        var access = await GetAssignmentAccessAsync(request.Id);
        return access.HasAssignments
            ? access.HasPrimary || access.HasSupport
            : HasLegacyOperationContributorRole();
    }

    private async Task<bool> CanCancelRequestAsync(OperationRequest request)
    {
        if (IsOperationAdmin() || request.RequestedByUserId == tenant.UserId) return true;
        var access = await GetAssignmentAccessAsync(request.Id);
        return access.HasAssignments
            ? access.HasPrimary
            : HasLegacyOperationManagerRole();
    }

    public async Task<OperationRequestListViewModel> GetListAsync(string? search, string? status, string? priority, Guid? deptId, int page = 1)
    {
        var tid = tenant.TenantId;
        var baseQ = db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted);

        var draftCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Draft);
        var submittedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview);
        var inProgressCount = await baseQ.CountAsync(r => r.Status == OperationStatus.InProgress);
        var completedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Completed);
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdueCount = await baseQ.CountAsync(r =>
            ((r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview) && r.ApprovalDueAt.HasValue && r.ApprovalDueAt < now)
            || ((r.Status == OperationStatus.Approved || r.Status == OperationStatus.InProgress || r.Status == OperationStatus.OnHold) && r.ResolutionDueAt.HasValue && r.ResolutionDueAt < now)
            || (!r.ApprovalDueAt.HasValue && !r.ResolutionDueAt.HasValue && r.DueDate.HasValue && r.DueDate.Value < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled));
        var criticalCount = await baseQ.CountAsync(r => r.Priority == PriorityLevel.Critical);
        var criticalOverdueCount = await baseQ.CountAsync(r =>
            r.Priority == PriorityLevel.Critical
            && (((r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview) && r.ApprovalDueAt.HasValue && r.ApprovalDueAt < now)
                || ((r.Status == OperationStatus.Approved || r.Status == OperationStatus.InProgress || r.Status == OperationStatus.OnHold) && r.ResolutionDueAt.HasValue && r.ResolutionDueAt < now)
                || (!r.ApprovalDueAt.HasValue && !r.ResolutionDueAt.HasValue && r.DueDate.HasValue && r.DueDate.Value < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled)));
        var overBudgetCount = await baseQ.CountAsync(r => r.CostVariancePercent.HasValue && r.CostVariancePercent > CostOverrunThresholdPercent);

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(r => r.Title.Contains(search) || r.RequestNo.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals(CriticalOverdueFilter, StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(r =>
                    r.Priority == PriorityLevel.Critical
                    && (((r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview) && r.ApprovalDueAt.HasValue && r.ApprovalDueAt < now)
                        || ((r.Status == OperationStatus.Approved || r.Status == OperationStatus.InProgress || r.Status == OperationStatus.OnHold) && r.ResolutionDueAt.HasValue && r.ResolutionDueAt < now)
                        || (!r.ApprovalDueAt.HasValue && !r.ResolutionDueAt.HasValue && r.DueDate.HasValue && r.DueDate.Value < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled)));
            }
            else if (status.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(r =>
                    ((r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview) && r.ApprovalDueAt.HasValue && r.ApprovalDueAt < now)
                    || ((r.Status == OperationStatus.Approved || r.Status == OperationStatus.InProgress || r.Status == OperationStatus.OnHold) && r.ResolutionDueAt.HasValue && r.ResolutionDueAt < now)
                    || (!r.ApprovalDueAt.HasValue && !r.ResolutionDueAt.HasValue && r.DueDate.HasValue && r.DueDate.Value < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled));
            }
            else if (status.Equals(OverBudgetFilter, StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(r => r.CostVariancePercent.HasValue && r.CostVariancePercent > CostOverrunThresholdPercent);
            }
            else if (Enum.TryParse<OperationStatus>(status, out var st))
            {
                q = q.Where(r => r.Status == st);
            }
        }
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<PriorityLevel>(priority, out var pr)) q = q.Where(r => r.Priority == pr);
        if (deptId.HasValue) q = q.Where(r => r.OrganizationUnitId == deptId.Value);

        var total = await q.CountAsync();
        var maxSlaDueAt = DateTimeOffset.MaxValue;
        var maxDueDate = DateOnly.MaxValue;
        var items = await q
            .OrderByDescending(r => r.Priority == PriorityLevel.Critical ? 4 : r.Priority == PriorityLevel.High ? 3 : r.Priority == PriorityLevel.Normal ? 2 : 1)
            .ThenBy(r => (r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview)
                ? (r.ApprovalDueAt ?? maxSlaDueAt)
                : (r.Status == OperationStatus.Approved || r.Status == OperationStatus.InProgress || r.Status == OperationStatus.OnHold)
                    ? (r.ResolutionDueAt ?? maxSlaDueAt)
                    : maxSlaDueAt)
            .ThenBy(r => r.DueDate ?? maxDueDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Join(db.AppUsers, r => r.RequestedByUserId, u => u.Id, (r, u) => new { r, u })
            .Join(db.OrganizationUnits, x => x.r.OrganizationUnitId, o => o.Id, (x, o) => new OperationRequestListItem
            {
                Id = x.r.Id, RequestNo = x.r.RequestNo, Title = x.r.Title, Type = x.r.Type, Status = x.r.Status.ToString(),
                Priority = x.r.Priority.ToString(), Department = o.Name, CreatedBy = x.u.FullName, CreatedAt = x.r.CreatedAt,
                DueDate = x.r.DueDate, TotalAmount = x.r.TotalAmount,
                EstimatedCost = x.r.EstimatedCost, ActualCost = x.r.ActualCost,
                CostVariance = x.r.CostVariance, CostVariancePercent = x.r.CostVariancePercent,
                PriorityWeight = x.r.Priority == PriorityLevel.Critical ? 4 : x.r.Priority == PriorityLevel.High ? 3 : x.r.Priority == PriorityLevel.Normal ? 2 : 1,
                ApprovalDueAt = x.r.ApprovalDueAt, ResolutionDueAt = x.r.ResolutionDueAt,
                SlaDueAt = OperationSlaService.GetActiveDueAt(x.r.Status, x.r.ApprovalDueAt, x.r.ResolutionDueAt),
                SlaStage = OperationSlaService.GetActiveStage(x.r.Status)
            })
            .ToListAsync();

        return new OperationRequestListViewModel
        {
            Items = items, TotalCount = total, Page = page,
            DraftCount = draftCount, SubmittedCount = submittedCount, InProgressCount = inProgressCount,
            CompletedCount = completedCount, OverdueCount = overdueCount,
            CriticalCount = criticalCount, CriticalOverdueCount = criticalOverdueCount,
            OverBudgetCount = overBudgetCount,
            SearchTerm = search, StatusFilter = status, PriorityFilter = priority, DeptFilter = deptId,
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync()
        };
    }

    public async Task<OperationRequestDetailViewModel?> GetDetailAsync(Guid id)
    {
        var r = await db.OperationRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (r is null) return null;
        var creator = await db.AppUsers.FindAsync(r.RequestedByUserId);
        var dept = await db.OrganizationUnits.FindAsync(r.OrganizationUnitId);
        var customer = r.CustomerId.HasValue ? await db.Customers.FindAsync(r.CustomerId.Value) : null;
        var customerSite = r.CustomerSiteId.HasValue ? await db.CustomerSites.FindAsync(r.CustomerSiteId.Value) : null;
        var lines = await db.Set<OperationRequestLine>().Where(l => l.OperationRequestId == id && !l.IsDeleted)
            .Select(l => new OrderLineDisplayItem
            {
                Id = l.Id, Quantity = l.Quantity, UnitPrice = l.UnitPrice, LineAmount = l.LineAmount, Note = l.Note,
                ProductName = l.ProductService != null ? l.ProductService.Name : null,
                ProductCode = l.ProductService != null ? l.ProductService.Code : null
            }).ToListAsync();
        var approvals = await db.ApprovalTasks.Where(t => t.TargetId == id && !t.IsDeleted)
            .Select(t => new ApprovalTaskItem { Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode, StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban lãnh đạo duyệt", Status = t.Status.ToString(), AssignedRole = t.AssignedRole, DecisionNote = t.DecisionNote, DecidedAt = t.DecidedAt }).ToListAsync();
        var workItems = await db.WorkItems.Where(w => w.OperationRequestId == id && !w.IsDeleted)
            .Select(w => new WorkItemListItem { Id = w.Id, Title = w.Title, Status = w.Status.ToString(), Priority = w.Priority.ToString(), DueDate = w.DueDate }).ToListAsync();
        var aiInsights = await db.AiInsights.Where(a => a.ContextId == id && a.TenantId == tenant.TenantId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt).Take(3)
            .Select(a => new AiInsightListItem { Id = a.Id, ContextType = a.ContextType, Question = a.Question, Summary = a.Summary, Recommendation = a.Recommendation, RiskLevel = a.RiskLevel.ToString(), Status = a.Status.ToString(), CreatedAt = a.CreatedAt }).ToListAsync();
        var activityLog = await db.AuditLogs
            .Where(a => a.TenantId == tenant.TenantId && a.EntityId == id && (a.EntityName == "OperationRequest" || a.EntityName == "ApprovalTask" || a.EntityName == "WorkItem"))
            .OrderByDescending(a => a.CreatedAt).Take(20)
            .Select(a => new ActivityLogItem { UserName = a.UserName, Action = a.Action, Details = a.NewValuesJson, OccurredAt = a.CreatedAt }).ToListAsync();

        var commentRows = await db.Set<OperationComment>()
            .AsNoTracking()
            .Where(c => c.OperationRequestId == id && c.TenantId == tenant.TenantId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Join(db.AppUsers.AsNoTracking(), c => c.AuthorUserId, u => u.Id, (c, u) => new OperationCommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                AuthorUserId = c.AuthorUserId,
                AuthorName = u.FullName,
                Type = c.Type,
                ParentCommentId = c.ParentCommentId,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
        var commentMap = commentRows.ToDictionary(c => c.Id);
        foreach (var comment in commentRows.Where(c => c.ParentCommentId.HasValue))
        {
            if (commentMap.TryGetValue(comment.ParentCommentId!.Value, out var parent))
            {
                parent.Replies.Add(comment);
            }
        }

        var comments = commentRows
            .Where(c => !c.ParentCommentId.HasValue || !commentMap.ContainsKey(c.ParentCommentId.Value))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var progressLogs = await db.OperationProgressLogs
            .Where(p => p.OperationRequestId == id && p.TenantId == tenant.TenantId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Join(db.AppUsers, p => p.CreatedByUserId!.Value, u => u.Id, (p, u) => new OperationProgressLogItem
            {
                Id = p.Id,
                ProgressPercent = p.ProgressPercent,
                Note = p.Note,
                CreatedByName = u.FullName,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var attachments = await db.Attachments
            .AsNoTracking()
            .Where(a => a.TenantId == tenant.TenantId
                && a.EntityName == OperationAttachmentService.OperationRequestEntityName
                && a.EntityId == id
                && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Join(db.AppUsers, a => a.UploadedByUserId, u => u.Id, (a, u) => new OperationAttachmentItem
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                UploadedByName = u.FullName,
                UploadedAt = a.CreatedAt
            })
            .ToListAsync();

        var assignmentEntities = await db.OperationRequestAssignments
            .AsNoTracking()
            .Include(a => a.AssignedUser)
            .Include(a => a.OrganizationUnit)
            .Where(a => a.OperationRequestId == id && a.TenantId == tenant.TenantId && a.IsActive && !a.IsDeleted)
            .OrderBy(a => a.Role)
            .ThenBy(a => a.AssignedAt)
            .ToListAsync();

        var assignments = assignmentEntities.Select(a => new OperationAssignmentItem
        {
            Id = a.Id,
            Role = a.Role,
            AssignedUserId = a.AssignedUserId,
            AssignedUserName = a.AssignedUser?.FullName,
            OrganizationUnitId = a.OrganizationUnitId,
            OrganizationUnitName = a.OrganizationUnit?.Name,
            AssignedAt = a.AssignedAt,
            Note = a.Note
        }).ToList();

        var assignmentAccess = await GetAssignmentAccessAsync(id);
        var canManageAssignments = CanManageOperationAssignments();
        var canManageWork = IsOperationAdmin()
            || (assignmentAccess.HasAssignments ? assignmentAccess.HasPrimary : HasLegacyOperationManagerRole());
        var canSupportWork = IsOperationAdmin()
            || (assignmentAccess.HasAssignments ? assignmentAccess.HasPrimary || assignmentAccess.HasSupport : HasLegacyOperationContributorRole());
        var canEditDraft = r.Status is OperationStatus.Draft or OperationStatus.Rejected
            && (r.RequestedByUserId == tenant.UserId || canSupportWork);
        var nextStates = OperationRequestStateMachine.NextStates(r.Status);
        var slaDueAt = OperationSlaService.GetActiveDueAt(r.Status, r.ApprovalDueAt, r.ResolutionDueAt);
        var currentProgress = progressLogs.FirstOrDefault()?.ProgressPercent
            ?? (r.Status == OperationStatus.Completed ? 100m : r.Status == OperationStatus.InProgress ? 5m : 0m);
        var progressStart = r.UpdatedAt ?? r.ApprovedAt ?? r.CreatedAt;
        var isProgressStale = r.Status == OperationStatus.InProgress
            && !progressLogs.Any()
            && DateTimeOffset.UtcNow - progressStart > TimeSpan.FromHours(48);

        return new OperationRequestDetailViewModel
        {
            Id = r.Id, RequestNo = r.RequestNo, Title = r.Title, Type = r.Type, Status = r.Status.ToString(), Priority = r.Priority.ToString(),
            Department = dept?.Name ?? "", DepartmentId = r.OrganizationUnitId, Customer = customer?.Name, CreatedBy = creator?.FullName ?? "",
            CreatedAt = r.CreatedAt, DueDate = r.DueDate, TotalAmount = r.TotalAmount, Description = r.Description,
            EstimatedCost = r.EstimatedCost, ActualCost = r.ActualCost,
            CostVariance = r.CostVariance, CostVariancePercent = r.CostVariancePercent,
            CostVarianceCalculatedAt = r.CostVarianceCalculatedAt,
            CustomerSiteName = customerSite?.Name,
            SubmittedAt = r.SubmittedAt, ApprovedAt = r.ApprovedAt,
            ApprovalDueAt = r.ApprovalDueAt, ResolutionDueAt = r.ResolutionDueAt,
            SlaDueAt = slaDueAt, SlaStage = OperationSlaService.GetActiveStage(r.Status),
            Lines = lines, ApprovalTasks = approvals, WorkItems = workItems, AiInsights = aiInsights, ActivityLog = activityLog,
            Comments = comments, ProgressLogs = progressLogs, Attachments = attachments, Assignments = assignments,
            CanEdit = canEditDraft,
            CanSubmit = nextStates.Contains(OperationStatus.Submitted) && await CanSubmitRequestAsync(r),
            CanCancel = nextStates.Contains(OperationStatus.Cancelled) && await CanCancelRequestAsync(r),
            CanStartWork = nextStates.Contains(OperationStatus.InProgress) && r.Status == OperationStatus.Approved && canManageWork,
            CanComplete = nextStates.Contains(OperationStatus.Completed) && canManageWork,
            CanManageWork = canManageWork,
            CanAddLine = ((r.Status is OperationStatus.Draft or OperationStatus.Rejected) && r.RequestedByUserId == tenant.UserId)
                || (canSupportWork && r.Status is not (OperationStatus.Completed or OperationStatus.Cancelled)),
            CanAddComment = canSupportWork && r.Status != OperationStatus.Cancelled,
            CanAddProgress = r.Status == OperationStatus.InProgress && canSupportWork,
            CanUploadAttachment = canSupportWork && r.Status != OperationStatus.Cancelled,
            CanManageAssignments = canManageAssignments,
            CurrentProgressPercent = currentProgress,
            LastProgressAt = progressLogs.FirstOrDefault()?.CreatedAt,
            IsProgressStale = isProgressStale,
            NextStatuses = nextStates.Select(s => s.ToString()).ToList(),
            AssignableUsers = await db.AppUsers
                .AsNoTracking()
                .Where(u => u.TenantId == tenant.TenantId && u.Status == UserStatus.Active && !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = string.IsNullOrWhiteSpace(u.JobTitle) ? u.FullName : u.FullName + " - " + u.JobTitle })
                .ToListAsync(),
            AssignableDepartments = await db.OrganizationUnits
                .AsNoTracking()
                .Where(o => o.TenantId == tenant.TenantId && o.IsActive && !o.IsDeleted)
                .OrderBy(o => o.Name)
                .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
                .ToListAsync()
        };
    }

    public async Task<Guid> CreateAsync(OperationRequestCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var now = DateTimeOffset.UtcNow;
        var requestYear = DateTime.Today.Year;
        var requestNo = await numbering.NextAsync(
            NumberingSequenceKeys.OperationRequest,
            $"OPR-{requestYear}-",
            3,
            requestYear);
        var validLines = NormalizeLineInputs(vm.Lines).ToList();
        var lineTotal = validLines.Sum(l => l.LineAmount);
        var entity = new OperationRequest
        {
            TenantId = tid, RequestNo = requestNo, Title = vm.Title, Type = vm.Type,
            OrganizationUnitId = vm.OrganizationUnitId, CustomerId = vm.CustomerId, Priority = vm.Priority,
            Status = OperationStatus.Draft, DueDate = vm.DueDate, Description = vm.Description,
            TotalAmount = vm.TotalAmount ?? (validLines.Any() ? lineTotal : null),
            RequestedByUserId = tenant.UserId, CreatedByUserId = tenant.UserId, CreatedAt = now
        };
        db.OperationRequests.Add(entity);
        foreach (var line in validLines)
        {
            db.OperationRequestLines.Add(new OperationRequestLine
            {
                TenantId = tid,
                OperationRequestId = entity.Id,
                ProductServiceId = line.ProductServiceId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineAmount = line.LineAmount,
                Note = line.Note,
                CreatedByUserId = tenant.UserId,
                CreatedAt = now
            });
        }

        db.OperationRequestAssignments.Add(new OperationRequestAssignment
        {
            TenantId = tid,
            OperationRequestId = entity.Id,
            OrganizationUnitId = vm.OrganizationUnitId,
            Role = OperationAssignmentRole.Primary,
            IsActive = true,
            AssignedAt = now,
            CreatedByUserId = tenant.UserId,
            CreatedAt = now
        });

        if (vm.TemplateId.HasValue)
        {
            var template = await db.OperationRequestTemplates
                .FirstOrDefaultAsync(t => t.Id == vm.TemplateId.Value && t.TenantId == tid && !t.IsDeleted);
            if (template != null)
            {
                template.UsageCount += 1;
                template.LastUsedAt = now;
                template.UpdatedAt = now;
                template.UpdatedByUserId = tenant.UserId;
            }
        }

        // Tự động sinh KPI hoặc OKR Nháp nếu Type tương ứng
        if (vm.Type == "KPI_PROPOSAL")
        {
            var kpi = new KpiDefinition
            {
                TenantId = tid, Code = $"KPI-{DateTime.Today:yyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                Name = vm.Title, Description = vm.Description, Status = KpiStatus.Draft,
                AssignerUserId = tenant.UserId, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow,
                OperationRequest = entity
            };
            db.KpiDefinitions.Add(kpi);
        }
        else if (vm.Type == "OKR_PROPOSAL")
        {
            var okr = new OkrObjective
            {
                TenantId = tid, ObjectiveName = vm.Title, Level = OkrLevel.Company, Status = OkrStatus.Draft,
                CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow,
                OperationRequest = entity
            };
            db.OkrObjectives.Add(okr);
        }

        await audit.LogAsync("OperationRequest", entity.Id, "Create", newValueObj: new { entity.RequestNo, entity.Title });
        await db.SaveChangesAsync();
        await cache.InvalidateTenantCacheAsync();
        return entity.Id;
    }

    public async Task<(bool Success, string Message, Guid? PlanId)> ConvertToPlanAsync(Guid id)
    {
        var tid = tenant.TenantId;
        var request = await db.OperationRequests
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.ProductService)
            .Include(r => r.Assignments.Where(a => a.IsActive && !a.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tid && !r.IsDeleted);

        if (request == null)
            return (false, "Yêu cầu vận hành không tồn tại hoặc bạn không có quyền truy cập.", null);

        if (!string.Equals(request.Type, "Project", StringComparison.OrdinalIgnoreCase))
            return (false, "Chỉ OperationRequest loại Project mới được chuyển thành kế hoạch vận hành.", null);

        if (request.Status != OperationStatus.Approved)
            return (false, "Chỉ yêu cầu Project đã được duyệt mới được chuyển thành kế hoạch vận hành.", null);

        if (!await CanManageRequestWorkAsync(id))
            return (false, "Bạn không có quyền chuyển yêu cầu này thành kế hoạch vận hành.", null);

        var existingPlan = await db.OperationPlans
            .Where(p => p.TenantId == tid && !p.IsDeleted && p.SourceOperationRequestId == id)
            .Select(p => new { p.Id, p.Code })
            .FirstOrDefaultAsync();
        if (existingPlan != null)
            return (true, $"Yêu cầu này đã có kế hoạch vận hành {existingPlan.Code}.", existingPlan.Id);

        var now = DateTimeOffset.UtcNow;
        var startDate = DateTime.Today;
        var endDate = request.DueDate.HasValue
            ? request.DueDate.Value.ToDateTime(new TimeOnly(17, 0))
            : startDate.AddDays(1);
        if (endDate <= startDate) endDate = startDate.AddDays(1);

        var planCode = await numbering.NextAsync(NumberingSequenceKeys.OperationPlan, "OPP-", 4);
        var primaryAssigneeId = request.Assignments
            .Where(a => a.Role == OperationAssignmentRole.Primary && a.AssignedUserId.HasValue)
            .OrderBy(a => a.AssignedAt)
            .Select(a => a.AssignedUserId)
            .FirstOrDefault();

        var plan = new OperationPlan
        {
            TenantId = tid,
            Code = planCode,
            Title = TruncateForPlan(request.Title, 200),
            PlanType = "Project",
            StartDate = startDate,
            EndDate = endDate,
            SourceOperationRequestId = request.Id,
            Status = OperationPlanStatus.Draft,
            Notes = BuildPlanNotes(request),
            CreatedByUserId = tenant.UserId,
            CreatedAt = now
        };
        db.OperationPlans.Add(plan);

        db.PlanTasks.Add(new PlanTask
        {
            TenantId = tid,
            PlanId = plan.Id,
            Name = BuildDefaultPlanTaskName(request),
            Description = BuildDefaultPlanTaskDescription(request),
            StartTime = startDate,
            EndTime = endDate,
            AssignedUserId = primaryAssigneeId,
            Status = PlanTaskStatus.Todo,
            ProgressPercent = 0,
            CreatedByUserId = tenant.UserId,
            CreatedAt = now
        });

        await audit.LogAsync("OperationRequest", request.Id, "ConvertToPlan",
            newValueObj: new { PlanId = plan.Id, plan.Code, plan.Title, plan.StartDate, plan.EndDate });
        await audit.LogAsync("OperationPlan", plan.Id, "CreateFromOperationRequest",
            newValueObj: new { plan.Code, plan.Title, plan.PlanType, plan.SourceOperationRequestId });

        var saved = await db.SaveChangesWithConcurrencyAsync();
        return saved
            ? (true, $"Đã chuyển yêu cầu {request.RequestNo} thành kế hoạch {plan.Code}.", plan.Id)
            : (false, ConcurrencySaveExtensions.StaleRecordMessage, null);
    }

    public async Task<List<Guid>> GetAssignmentNotificationUserIdsAsync(Guid requestId)
    {
        var tid = tenant.TenantId;
        var assignments = await db.OperationRequestAssignments
            .AsNoTracking()
            .Where(a => a.TenantId == tid
                && a.OperationRequestId == requestId
                && a.IsActive
                && !a.IsDeleted
                && (a.Role == OperationAssignmentRole.Primary || a.Role == OperationAssignmentRole.Watcher))
            .Select(a => new { a.AssignedUserId, a.OrganizationUnitId })
            .ToListAsync();

        if (!assignments.Any()) return new();

        var directUserIds = assignments
            .Where(a => a.AssignedUserId.HasValue)
            .Select(a => a.AssignedUserId!.Value)
            .Distinct()
            .ToList();

        var userIds = directUserIds.Any()
            ? await db.AppUsers
                .AsNoTracking()
                .Where(u => u.TenantId == tid
                    && directUserIds.Contains(u.Id)
                    && u.Status == UserStatus.Active
                    && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync()
            : new List<Guid>();

        var departmentIds = assignments
            .Where(a => a.OrganizationUnitId.HasValue)
            .Select(a => a.OrganizationUnitId!.Value)
            .Distinct()
            .ToList();

        if (departmentIds.Any())
        {
            var primaryDepartmentUsers = await db.AppUsers
                .AsNoTracking()
                .Where(u => u.TenantId == tid
                    && u.OrganizationUnitId.HasValue
                    && departmentIds.Contains(u.OrganizationUnitId.Value)
                    && u.Status == UserStatus.Active
                    && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var employeeDepartmentUsers = await db.EmployeeDepartmentAssignments
                .AsNoTracking()
                .Where(a => a.TenantId == tid
                    && departmentIds.Contains(a.OrganizationUnitId)
                    && !a.IsDeleted
                    && a.EmployeeProfile != null
                    && a.EmployeeProfile.User != null
                    && a.EmployeeProfile.User.Status == UserStatus.Active
                    && !a.EmployeeProfile.User.IsDeleted
                    && a.EffectiveFrom <= today
                    && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value >= today))
                .Select(a => a.EmployeeProfile!.UserId)
                .ToListAsync();

            userIds.AddRange(primaryDepartmentUsers);
            userIds.AddRange(employeeDepartmentUsers);
        }

        return userIds.Distinct().ToList();
    }

    public async Task<List<Guid>> GetCancelNotificationUserIdsAsync(Guid requestId)
    {
        var request = await db.OperationRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId && r.TenantId == tenant.TenantId && !r.IsDeleted)
            .Select(r => new { r.RequestedByUserId })
            .FirstOrDefaultAsync();
        if (request is null) return [];

        var recipientIds = await GetAssignmentNotificationUserIdsAsync(requestId);
        recipientIds.Add(request.RequestedByUserId);
        return recipientIds.Distinct().ToList();
    }

    public async Task<(bool Success, string Message)> AddAssignmentAsync(OperationAssignmentInputViewModel vm)
    {
        if (!CanManageOperationAssignments()) return (false, "Bạn không có quyền phân công yêu cầu này.");

        var r = await db.OperationRequests
            .FirstOrDefaultAsync(x => x.Id == vm.OperationRequestId && x.TenantId == tenant.TenantId && !x.IsDeleted);
        if (r is null) return (false, "Không tìm thấy yêu cầu.");

        if (!Enum.IsDefined(typeof(OperationAssignmentRole), vm.Role))
            return (false, "Vai trò phân công không hợp lệ.");

        var hasUser = vm.AssignedUserId.HasValue;
        var hasDepartment = vm.OrganizationUnitId.HasValue;
        if (hasUser == hasDepartment)
            return (false, "Chọn một người phụ trách hoặc một phòng ban.");

        if (vm.AssignedUserId is Guid assignedUserId)
        {
            var userExists = await db.AppUsers.AnyAsync(u => u.Id == assignedUserId
                && u.TenantId == tenant.TenantId
                && u.Status == UserStatus.Active
                && !u.IsDeleted);
            if (!userExists) return (false, "Người được phân công không hợp lệ.");
        }

        if (vm.OrganizationUnitId is Guid organizationUnitId)
        {
            var departmentExists = await db.OrganizationUnits.AnyAsync(o => o.Id == organizationUnitId
                && o.TenantId == tenant.TenantId
                && o.IsActive
                && !o.IsDeleted);
            if (!departmentExists) return (false, "Phòng ban được phân công không hợp lệ.");
        }

        var duplicate = await db.OperationRequestAssignments.AnyAsync(a =>
            a.TenantId == tenant.TenantId
            && a.OperationRequestId == r.Id
            && a.Role == vm.Role
            && a.AssignedUserId == vm.AssignedUserId
            && a.OrganizationUnitId == vm.OrganizationUnitId
            && a.IsActive
            && !a.IsDeleted);
        if (duplicate) return (false, "Phân công này đã tồn tại.");

        var now = DateTimeOffset.UtcNow;
        var note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim();
        db.OperationRequestAssignments.Add(new OperationRequestAssignment
        {
            TenantId = tenant.TenantId,
            OperationRequestId = r.Id,
            Role = vm.Role,
            AssignedUserId = vm.AssignedUserId,
            OrganizationUnitId = vm.OrganizationUnitId,
            IsActive = true,
            AssignedAt = now,
            Note = note,
            CreatedByUserId = tenant.UserId,
            CreatedAt = now
        });

        await audit.LogAsync("OperationRequest", r.Id, "AddAssignment",
            newValueObj: new { vm.Role, vm.AssignedUserId, vm.OrganizationUnitId, Note = note });
        return await db.SaveChangesWithConcurrencyAsync()
            ? (true, "Đã thêm phân công.")
            : (false, "Không thể thêm phân công do dữ liệu đã thay đổi.");
    }

    public async Task<(bool Success, string Message)> RemoveAssignmentAsync(Guid assignmentId)
    {
        if (!CanManageOperationAssignments()) return (false, "Bạn không có quyền xóa phân công.");

        var assignment = await db.OperationRequestAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenant.TenantId && !a.IsDeleted);
        if (assignment is null) return (false, "Không tìm thấy phân công.");

        assignment.IsActive = false;
        assignment.IsDeleted = true;
        assignment.UpdatedAt = DateTimeOffset.UtcNow;
        assignment.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync("OperationRequest", assignment.OperationRequestId, "RemoveAssignment",
            oldValueObj: new { assignment.Role, assignment.AssignedUserId, assignment.OrganizationUnitId });
        return await db.SaveChangesWithConcurrencyAsync()
            ? (true, "Đã xóa phân công.")
            : (false, "Không thể xóa phân công do dữ liệu đã thay đổi.");
    }

    public async Task<OperationRequestEditViewModel?> GetEditFormAsync(Guid id)
    {
        var r = await db.OperationRequests.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (r is null || r.Status is not (OperationStatus.Draft or OperationStatus.Rejected)) return null;
        if (r.RequestedByUserId != tenant.UserId && !await CanSupportRequestAsync(r.Id)) return null;

        var tid = tenant.TenantId;
        return new OperationRequestEditViewModel
        {
            Id = r.Id, RequestNo = r.RequestNo, Title = r.Title, Type = r.Type,
            OrganizationUnitId = r.OrganizationUnitId, CustomerId = r.CustomerId,
            Priority = r.Priority, DueDate = r.DueDate, Description = r.Description, TotalAmount = r.TotalAmount,
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted).Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync(),
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted).Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name }).ToListAsync()
        };
    }

    public async Task<bool> UpdateAsync(OperationRequestEditViewModel vm)
    {
        var r = await db.OperationRequests.FindAsync(vm.Id);
        if (r is null || r.TenantId != tenant.TenantId || r.Status is not (OperationStatus.Draft or OperationStatus.Rejected)) return false;
        if (r.RequestedByUserId != tenant.UserId && !await CanSupportRequestAsync(r.Id)) return false;

        var oldTitle = r.Title;
        r.Title = vm.Title; r.Type = vm.Type; r.OrganizationUnitId = vm.OrganizationUnitId;
        r.CustomerId = vm.CustomerId; r.Priority = vm.Priority; r.DueDate = vm.DueDate;
        r.Description = vm.Description; r.TotalAmount = vm.TotalAmount;
        r.UpdatedAt = DateTimeOffset.UtcNow; r.UpdatedByUserId = tenant.UserId;

        // If rejected, allow resubmission by resetting to Draft
        if (OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.Draft)) r.Status = OperationStatus.Draft;

        await audit.LogAsync("OperationRequest", r.Id, "Update",
            oldValueObj: new { Title = oldTitle },
            newValueObj: new { r.Title, r.Priority });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }

    public async Task<bool> SubmitAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null
            || r.TenantId != tenant.TenantId
            || !OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.Submitted)
            || !await CanSubmitRequestAsync(r)) return false;
        var oldStatus = r.Status;
        var submittedAt = DateTimeOffset.UtcNow;
        r.Status = OperationStatus.Submitted; r.UpdatedAt = submittedAt;
        await operationSla.ApplySubmittedAsync(r, submittedAt);
        operationApprovals.CreateDepartmentReviewTask(id, submittedAt);
        await audit.LogAsync("OperationRequest", id, "Submit",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = OperationStatus.Submitted, r.ApprovalDueAt });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved)
        {
            slaWatcherQueue.TryQueue("operation-request-submitted");
            await cache.InvalidateTenantCacheAsync();
        }
        return saved;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null
            || r.TenantId != tenant.TenantId
            || !OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.Cancelled)
            || !await CanCancelRequestAsync(r)) return false;
        var oldStatus = r.Status;
        r.Status = OperationStatus.Cancelled; r.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync("OperationRequest", id, "Cancel",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = OperationStatus.Cancelled });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId) return false;
        r.IsDeleted = true; r.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync("OperationRequest", id, "Delete");
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }

    public async Task<OperationRequestCreateViewModel> GetCreateFormAsync(Guid? templateId = null)
    {
        var tid = tenant.TenantId;
        var vm = new OperationRequestCreateViewModel
        {
            TemplateId = templateId,
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted).Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync(),
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted).Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Code + " — " + c.Name }).ToListAsync(),
            Products = await db.ProductServices.Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted).OrderBy(p => p.Name)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.Code + " — " + p.Name + (p.StandardPrice.HasValue ? $" ({p.StandardPrice:N0}₫)" : "") }).ToListAsync(),
            Templates = await db.OperationRequestTemplates
                .Where(t => t.TenantId == tid && t.IsActive && !t.IsDeleted)
                .OrderBy(t => t.Title)
                .Select(t => new SelectOption { Value = t.Id.ToString(), Text = t.Title })
                .ToListAsync()
        };

        if (templateId.HasValue)
        {
            var template = await db.OperationRequestTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId.Value && t.TenantId == tid && t.IsActive && !t.IsDeleted);
            if (template != null)
            {
                vm.Title = template.Title;
                vm.Type = template.Type;
                vm.Priority = template.Priority;
                vm.OrganizationUnitId = template.DefaultDepartmentId;
                vm.Description = template.Description;
                vm.Lines = await BuildLineInputsFromTemplateAsync(template.DefaultLinesJson);
                if (vm.Lines.Any()) vm.TotalAmount = vm.Lines.Sum(l => l.LineAmount);
            }
        }

        return vm;
    }

    public async Task<bool> StartWorkAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null
            || r.TenantId != tenant.TenantId
            || !OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.InProgress)
            || !await CanManageRequestWorkAsync(id)) return false;
        var oldStatus = r.Status;
        r.Status = OperationStatus.InProgress; r.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync("OperationRequest", id, "StartWork",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = OperationStatus.InProgress });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }

    public async Task<bool> CompleteAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null
            || r.TenantId != tenant.TenantId
            || !OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.Completed)
            || !await CanManageRequestWorkAsync(id)) return false;
        var oldStatus = r.Status;
        var completedAt = DateTimeOffset.UtcNow;
        r.Status = OperationStatus.Completed; r.UpdatedAt = completedAt;
        await ApplyCostVarianceAsync(r, completedAt);
        await audit.LogAsync("OperationRequest", id, "Complete",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = OperationStatus.Completed, r.EstimatedCost, r.ActualCost, r.CostVariance, r.CostVariancePercent });
        if (r.CostVariancePercent.HasValue && r.CostVariancePercent.Value > CostOverrunThresholdPercent)
        {
            await audit.LogAsync("OperationRequest", id, "CostOverrun",
                newValueObj: new { r.EstimatedCost, r.ActualCost, r.CostVariance, r.CostVariancePercent });
        }
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }

    private async Task ApplyCostVarianceAsync(OperationRequest request, DateTimeOffset calculatedAt)
    {
        var estimated = request.EstimatedCost ?? await CalculateEstimatedCostAsync(request.Id);
        var goodsIssueCost = await CalculateConfirmedGoodsIssueCostAsync(request.Id);
        var paymentCost = await db.PaymentRequests
            .Where(p => p.TenantId == tenant.TenantId
                && p.OperationRequestId == request.Id
                && !p.IsDeleted
                && (p.Status == PaymentStatus.Approved || p.Status == PaymentStatus.Paid))
            .SumAsync(p => p.TotalAmount);

        var actual = goodsIssueCost + paymentCost;
        var variance = actual - estimated;
        var variancePercent = estimated > 0
            ? Math.Round(variance / estimated * 100m, 2)
            : actual > 0 ? 100m : 0m;

        request.EstimatedCost = estimated;
        request.ActualCost = actual;
        request.CostVariance = variance;
        request.CostVariancePercent = variancePercent;
        request.CostVarianceCalculatedAt = calculatedAt;
    }

    private async Task<decimal> CalculateEstimatedCostAsync(Guid requestId)
    {
        var lines = await db.OperationRequestLines
            .Where(l => l.TenantId == tenant.TenantId && l.OperationRequestId == requestId && !l.IsDeleted)
            .Select(l => new { l.Quantity, l.UnitPrice, l.LineAmount })
            .ToListAsync();

        return lines.Sum(l => l.LineAmount ?? l.Quantity * (l.UnitPrice ?? 0m));
    }

    private async Task<decimal> CalculateConfirmedGoodsIssueCostAsync(Guid requestId)
    {
        var lines = await db.GoodsIssueLines
            .Where(l => l.TenantId == tenant.TenantId
                && !l.IsDeleted
                && l.GoodsIssue != null
                && l.GoodsIssue.TenantId == tenant.TenantId
                && !l.GoodsIssue.IsDeleted
                && l.GoodsIssue.OperationRequestId == requestId
                && l.GoodsIssue.Status == GoodsIssueStatus.Confirmed)
            .Select(l => new
            {
                l.IssuedQuantity,
                l.UnitCost,
                l.LineAmount,
                StandardPrice = l.ProductService != null ? l.ProductService.StandardPrice : null
            })
            .ToListAsync();

        return lines.Sum(l => l.LineAmount ?? l.IssuedQuantity * (l.UnitCost ?? l.StandardPrice ?? 0m));
    }

    public async Task<Guid> AddLineAsync(Guid requestId, OrderLineInputViewModel input)
    {
        var r = await db.OperationRequests.FindAsync(requestId);
        var canAddLine = r is not null
            && ((r.Status is OperationStatus.Draft or OperationStatus.Rejected && r.RequestedByUserId == tenant.UserId)
                || await CanSupportRequestAsync(requestId));
        if (r is null
            || r.TenantId != tenant.TenantId
            || r.Status is OperationStatus.Completed or OperationStatus.Cancelled
            || !canAddLine) return Guid.Empty;

        var line = new OperationRequestLine
        {
            TenantId = tenant.TenantId, OperationRequestId = requestId,
            ProductServiceId = input.ProductServiceId, Quantity = input.Quantity,
            UnitPrice = input.UnitPrice, LineAmount = input.Quantity * (input.UnitPrice ?? 0),
            Note = input.Note, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = tenant.UserId
        };
        db.Set<OperationRequestLine>().Add(line);
        var existingTotal = await db.Set<OperationRequestLine>().Where(l => l.OperationRequestId == requestId && !l.IsDeleted).SumAsync(l => l.LineAmount ?? 0);
        r.TotalAmount = existingTotal + (line.LineAmount ?? 0);
        await audit.LogAsync("OperationRequest", requestId, "AddLine",
            newValueObj: new { input.ProductServiceId, input.Quantity, input.UnitPrice, input.Note });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        if (!saved) return Guid.Empty;
        return line.Id;
    }

    public async Task<bool> RemoveLineAsync(Guid lineId)
    {
        var line = await db.Set<OperationRequestLine>().FindAsync(lineId);
        if (line is null || line.TenantId != tenant.TenantId) return false;
        var r = await db.OperationRequests.FindAsync(line.OperationRequestId);
        if (r is null) return false;
        var canEditDraftLine = r.Status is OperationStatus.Draft or OperationStatus.Rejected
            && (r.RequestedByUserId == tenant.UserId || await CanSupportRequestAsync(r.Id));
        if (!canEditDraftLine && !await CanManageRequestWorkAsync(r.Id)) return false;
        line.IsDeleted = true; line.UpdatedAt = DateTimeOffset.UtcNow;
        // Recalculate total
        r.TotalAmount = await db.Set<OperationRequestLine>().Where(l => l.OperationRequestId == line.OperationRequestId && !l.IsDeleted && l.Id != lineId).SumAsync(l => l.LineAmount ?? 0);
        await audit.LogAsync("OperationRequest", r.Id, "RemoveLine",
            oldValueObj: new { line.ProductServiceId, line.Quantity, line.UnitPrice, line.Note });
        return await db.SaveChangesWithConcurrencyAsync();
    }

    public async Task<OperationRequestTemplateListViewModel> GetTemplatesAsync(string? search = null)
    {
        var tid = tenant.TenantId;
        var query = db.OperationRequestTemplates
            .AsNoTracking()
            .Include(t => t.DefaultDepartment)
            .Where(t => t.TenantId == tid && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || t.Type.Contains(search));

        var templates = await query
            .OrderByDescending(t => t.IsActive)
            .ThenByDescending(t => t.UsageCount)
            .ThenBy(t => t.Title)
            .ToListAsync();

        return new OperationRequestTemplateListViewModel
        {
            SearchTerm = search,
            Items = templates.Select(t => new OperationRequestTemplateItem
            {
                Id = t.Id,
                Title = t.Title,
                Type = t.Type,
                Priority = t.Priority.ToString(),
                Department = t.DefaultDepartment?.Name ?? "",
                DefaultLineCount = DeserializeTemplateLines(t.DefaultLinesJson).Count,
                IsActive = t.IsActive,
                UsageCount = t.UsageCount,
                CreatedAt = t.CreatedAt,
                LastUsedAt = t.LastUsedAt
            }).ToList()
        };
    }

    public async Task<OperationRequestTemplateEditViewModel?> GetTemplateFormAsync(Guid? id = null)
    {
        var tid = tenant.TenantId;
        var departments = await GetDepartmentOptionsAsync(tid);

        if (!id.HasValue)
            return new OperationRequestTemplateEditViewModel { Departments = departments, IsActive = true };

        var template = await db.OperationRequestTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id.Value && t.TenantId == tid && !t.IsDeleted);
        if (template is null) return null;

        return new OperationRequestTemplateEditViewModel
        {
            Id = template.Id,
            Title = template.Title,
            Type = template.Type,
            Priority = template.Priority,
            DefaultDepartmentId = template.DefaultDepartmentId,
            Description = template.Description,
            DefaultLinesJson = template.DefaultLinesJson,
            IsActive = template.IsActive,
            Departments = departments
        };
    }

    public async Task<(bool Success, string Message, Guid? Id)> CreateTemplateAsync(OperationRequestTemplateEditViewModel vm)
    {
        if (!TryNormalizeTemplateLinesJson(vm.DefaultLinesJson, out var normalizedLinesJson))
            return (false, "DefaultLines JSON không hợp lệ.", null);

        var now = DateTimeOffset.UtcNow;
        var template = new OperationRequestTemplate
        {
            TenantId = tenant.TenantId,
            Title = vm.Title.Trim(),
            Type = vm.Type.Trim(),
            Priority = vm.Priority,
            DefaultDepartmentId = vm.DefaultDepartmentId,
            Description = vm.Description,
            DefaultLinesJson = normalizedLinesJson,
            IsActive = vm.IsActive,
            CreatedAt = now,
            CreatedByUserId = tenant.UserId
        };

        db.OperationRequestTemplates.Add(template);
        await audit.LogAsync("OperationRequestTemplate", template.Id, "Create", newValueObj: new { template.Title, template.Type, template.Priority });
        await db.SaveChangesAsync();
        return (true, "Đã tạo template.", template.Id);
    }

    public async Task<(bool Success, string Message)> UpdateTemplateAsync(OperationRequestTemplateEditViewModel vm)
    {
        if (!TryNormalizeTemplateLinesJson(vm.DefaultLinesJson, out var normalizedLinesJson))
            return (false, "DefaultLines JSON không hợp lệ.");

        var template = await db.OperationRequestTemplates
            .FirstOrDefaultAsync(t => t.Id == vm.Id && t.TenantId == tenant.TenantId && !t.IsDeleted);
        if (template is null) return (false, "Không tìm thấy template.");

        var oldValue = new { template.Title, template.Type, template.Priority, template.IsActive };
        template.Title = vm.Title.Trim();
        template.Type = vm.Type.Trim();
        template.Priority = vm.Priority;
        template.DefaultDepartmentId = vm.DefaultDepartmentId;
        template.Description = vm.Description;
        template.DefaultLinesJson = normalizedLinesJson;
        template.IsActive = vm.IsActive;
        template.UpdatedAt = DateTimeOffset.UtcNow;
        template.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync("OperationRequestTemplate", template.Id, "Update",
            oldValueObj: oldValue,
            newValueObj: new { template.Title, template.Type, template.Priority, template.IsActive });
        return await db.SaveChangesWithConcurrencyAsync()
            ? (true, "Đã cập nhật template.")
            : (false, "Không thể cập nhật template do dữ liệu đã thay đổi.");
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var template = await db.OperationRequestTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenant.TenantId && !t.IsDeleted);
        if (template is null) return false;

        template.IsDeleted = true;
        template.IsActive = false;
        template.UpdatedAt = DateTimeOffset.UtcNow;
        template.UpdatedByUserId = tenant.UserId;
        await audit.LogAsync("OperationRequestTemplate", id, "Delete");
        return await db.SaveChangesWithConcurrencyAsync();
    }

    public async Task<(bool Success, string Message, Guid? TemplateId)> CreateTemplateFromRequestAsync(Guid requestId)
    {
        var request = await db.OperationRequests
            .AsNoTracking()
            .Include(r => r.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == requestId && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (request is null) return (false, "Không tìm thấy yêu cầu.", null);
        if (request.Status != OperationStatus.Completed) return (false, "Chỉ lưu template từ yêu cầu đã hoàn thành.", null);

        var linesJson = SerializeTemplateLines(request.Lines.Select(l => new OrderLineInputViewModel
        {
            ProductServiceId = l.ProductServiceId,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            Note = l.Note
        }));

        var now = DateTimeOffset.UtcNow;
        var template = new OperationRequestTemplate
        {
            TenantId = tenant.TenantId,
            Title = request.Title,
            Type = request.Type,
            Priority = request.Priority,
            DefaultDepartmentId = request.OrganizationUnitId,
            Description = request.Description,
            DefaultLinesJson = linesJson,
            IsActive = true,
            CreatedAt = now,
            CreatedByUserId = tenant.UserId
        };

        db.OperationRequestTemplates.Add(template);
        await audit.LogAsync("OperationRequestTemplate", template.Id, "CreateFromRequest",
            newValueObj: new { template.Title, OperationRequestId = requestId });
        await db.SaveChangesAsync();
        return (true, "Đã lưu yêu cầu thành template.", template.Id);
    }

    private async Task<List<SelectOption>> GetDepartmentOptionsAsync(Guid tenantId) =>
        await db.OrganizationUnits
            .Where(o => o.TenantId == tenantId && o.IsActive && !o.IsDeleted)
            .OrderBy(o => o.Name)
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

    private async Task<List<OrderLineInputViewModel>> BuildLineInputsFromTemplateAsync(string? defaultLinesJson)
    {
        var definitions = DeserializeTemplateLines(defaultLinesJson);
        var productIds = definitions.Where(l => l.ProductServiceId.HasValue).Select(l => l.ProductServiceId!.Value).Distinct().ToList();
        var productNames = productIds.Any()
            ? await db.ProductServices
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name)
            : new Dictionary<Guid, string>();

        return definitions.Select(l => new OrderLineInputViewModel
        {
            ProductServiceId = l.ProductServiceId,
            ProductName = l.ProductServiceId.HasValue && productNames.TryGetValue(l.ProductServiceId.Value, out var productName)
                ? productName
                : l.ProductName,
            Quantity = l.Quantity <= 0 ? 1 : l.Quantity,
            UnitPrice = l.UnitPrice,
            Note = l.Note
        }).ToList();
    }

    private static IEnumerable<OrderLineInputViewModel> NormalizeLineInputs(IEnumerable<OrderLineInputViewModel>? lines)
    {
        return (lines ?? [])
            .Where(l => l.Quantity > 0
                && (l.ProductServiceId.HasValue
                    || l.UnitPrice.HasValue
                    || !string.IsNullOrWhiteSpace(l.ProductName)
                    || !string.IsNullOrWhiteSpace(l.Note)))
            .Select(l => new OrderLineInputViewModel
            {
                ProductServiceId = l.ProductServiceId,
                ProductName = string.IsNullOrWhiteSpace(l.ProductName) ? null : l.ProductName.Trim(),
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                Note = string.IsNullOrWhiteSpace(l.Note)
                    ? string.IsNullOrWhiteSpace(l.ProductName) ? null : l.ProductName.Trim()
                    : l.Note.Trim()
            });
    }

    private static string TruncateForPlan(string value, int maxLength)
    {
        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string BuildDefaultPlanTaskName(OperationRequest request)
    {
        var firstLineName = request.Lines
            .Where(l => !l.IsDeleted)
            .Select(GetLineName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

        var name = string.IsNullOrWhiteSpace(firstLineName)
            ? $"Thực hiện {request.RequestNo}"
            : $"Thực hiện {firstLineName}";
        return TruncateForPlan(name, 200);
    }

    private static string BuildPlanNotes(OperationRequest request)
    {
        var notes = $"Sinh từ OperationRequest {request.RequestNo}.";
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            notes += Environment.NewLine + request.Description.Trim();
        }

        return notes;
    }

    private static string BuildDefaultPlanTaskDescription(OperationRequest request)
    {
        var lines = request.Lines
            .Where(l => !l.IsDeleted)
            .OrderBy(l => l.CreatedAt)
            .Select((line, index) => $"{index + 1}. {BuildLineSummary(line)}")
            .ToList();

        var description = $"Task mặc định sinh từ yêu cầu {request.RequestNo}.";
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            description += Environment.NewLine + request.Description.Trim();
        }

        if (lines.Any())
        {
            description += Environment.NewLine + "Lines:" + Environment.NewLine + string.Join(Environment.NewLine, lines);
        }

        return description;
    }

    private static string BuildLineSummary(OperationRequestLine line)
    {
        var name = GetLineName(line);
        var quantity = line.Quantity > 0 ? $"SL {line.Quantity:0.##}" : "SL N/A";
        return string.IsNullOrWhiteSpace(line.Note)
            ? $"{name} ({quantity})"
            : $"{name} ({quantity}) - {line.Note.Trim()}";
    }

    private static string GetLineName(OperationRequestLine line)
    {
        if (!string.IsNullOrWhiteSpace(line.ProductService?.Name)) return line.ProductService.Name;
        if (!string.IsNullOrWhiteSpace(line.ProductService?.Code)) return line.ProductService.Code;
        if (!string.IsNullOrWhiteSpace(line.Note)) return line.Note.Trim();
        return "Hạng mục yêu cầu";
    }

    private static string? SerializeTemplateLines(IEnumerable<OrderLineInputViewModel>? lines)
    {
        var definitions = NormalizeLineInputs(lines)
            .Select(l => new OperationRequestTemplateLineDefinition
            {
                ProductServiceId = l.ProductServiceId,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                Note = l.Note
            })
            .ToList();

        return definitions.Any() ? JsonSerializer.Serialize(definitions, TemplateJsonOptions) : null;
    }

    private static bool TryNormalizeTemplateLinesJson(string? json, out string? normalizedJson)
    {
        normalizedJson = null;
        if (string.IsNullOrWhiteSpace(json)) return true;

        try
        {
            var definitions = JsonSerializer.Deserialize<List<OperationRequestTemplateLineDefinition>>(json, TemplateJsonOptions) ?? [];
            var validDefinitions = definitions
                .Where(l => (l.Quantity <= 0 ? 1 : l.Quantity) > 0
                    && (l.ProductServiceId.HasValue
                        || l.UnitPrice.HasValue
                        || !string.IsNullOrWhiteSpace(l.ProductName)
                        || !string.IsNullOrWhiteSpace(l.Note)))
                .Select(l => new OperationRequestTemplateLineDefinition
                {
                    ProductServiceId = l.ProductServiceId,
                    ProductName = string.IsNullOrWhiteSpace(l.ProductName) ? null : l.ProductName.Trim(),
                    Quantity = l.Quantity <= 0 ? 1 : l.Quantity,
                    UnitPrice = l.UnitPrice,
                    Note = string.IsNullOrWhiteSpace(l.Note) ? null : l.Note.Trim()
                })
                .ToList();

            normalizedJson = validDefinitions.Any()
                ? JsonSerializer.Serialize(validDefinitions, TemplateJsonOptions)
                : null;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static List<OperationRequestTemplateLineDefinition> DeserializeTemplateLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<OperationRequestTemplateLineDefinition>>(json, TemplateJsonOptions)?
                .Where(l => l.Quantity > 0)
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task<OperationStatisticsViewModel> GetStatisticsAsync()
    {
        var tid = tenant.TenantId;
        var baseQ = db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted);

        var total = await baseQ.CountAsync();
        var completed = await baseQ.CountAsync(r => r.Status == OperationStatus.Completed);
        var cancelled = await baseQ.CountAsync(r => r.Status == OperationStatus.Cancelled);
        var activeTotal = total - cancelled;

        decimal completionRate = activeTotal > 0 ? (decimal)completed / activeTotal * 100 : 0;

        var completedRequests = await baseQ.Where(r => r.Status == OperationStatus.Completed).ToListAsync();
        double avgProcessingDays = 0;
        if (completedRequests.Any())
        {
            avgProcessingDays = completedRequests.Average(r => {
                var end = r.UpdatedAt ?? DateTimeOffset.UtcNow;
                return (end - r.CreatedAt).TotalDays;
            });
        }

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var requestsWithDue = await baseQ
            .Where(r => r.Status != OperationStatus.Cancelled
                && (r.ApprovalDueAt.HasValue || r.ResolutionDueAt.HasValue || r.DueDate.HasValue))
            .ToListAsync();
        decimal slaComplianceRate = 100;
        if (requestsWithDue.Any())
        {
            int compliantCount = requestsWithDue.Count(r => {
                var slaDueAt = r.Status == OperationStatus.Completed
                    ? r.ResolutionDueAt ?? r.ApprovalDueAt
                    : OperationSlaService.GetActiveDueAt(r.Status, r.ApprovalDueAt, r.ResolutionDueAt);

                if (slaDueAt.HasValue)
                {
                    var checkpoint = r.Status == OperationStatus.Completed
                        ? r.UpdatedAt ?? now
                        : now;
                    return checkpoint <= slaDueAt.Value;
                }

                if (!r.DueDate.HasValue) return true;

                if (r.Status == OperationStatus.Completed)
                {
                    var compDate = r.UpdatedAt.HasValue ? DateOnly.FromDateTime(r.UpdatedAt.Value.Date) : DateOnly.FromDateTime(DateTime.Today);
                    return compDate <= r.DueDate.Value;
                }
                return today <= r.DueDate.Value;
            });
            slaComplianceRate = (decimal)compliantCount / requestsWithDue.Count * 100;
        }

        var priorityGroup = await baseQ.GroupBy(r => r.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToListAsync();

        var priorityList = priorityGroup.Select(g => new PriorityStatItem
        {
            Priority = g.Priority.ToString(),
            PriorityLabel = g.Priority switch
            {
                PriorityLevel.Low => "Thấp",
                PriorityLevel.Normal => "Bình thường",
                PriorityLevel.High => "Cao",
                PriorityLevel.Critical => "Nghiêm trọng",
                _ => g.Priority.ToString()
            },
            Count = g.Count
        }).ToList();

        var deptGroup = await baseQ
            .Join(db.OrganizationUnits, r => r.OrganizationUnitId, o => o.Id, (r, o) => new { r, o })
            .GroupBy(x => x.o.Name)
            .Select(g => new DepartmentStatItem
            {
                DepartmentName = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var weeklyTrend = new List<WeeklyTrendItem>();
        var todayDate = DateTime.Today;
        for (int i = 6; i >= 0; i--)
        {
            var date = todayDate.AddDays(-i);
            var dateOnly = DateOnly.FromDateTime(date);
            
            var createdCount = await baseQ.CountAsync(r => r.CreatedAt.Date == date);
            var completedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Completed && r.UpdatedAt.HasValue && r.UpdatedAt.Value.Date == date);

            weeklyTrend.Add(new WeeklyTrendItem
            {
                DateLabel = date.ToString("dd/MM"),
                CreatedCount = createdCount,
                CompletedCount = completedCount
            });
        }

        return new OperationStatisticsViewModel
        {
            CompletionRate = Math.Round(completionRate, 1),
            AvgProcessingDays = Math.Round(avgProcessingDays, 1),
            SlaComplianceRate = Math.Round(slaComplianceRate, 1),
            ByPriority = priorityList,
            ByDepartment = deptGroup,
            WeeklyTrend = weeklyTrend
        };
    }

    public async Task<(bool Success, string Message, IReadOnlyCollection<Guid> MentionedUserIds)> AddCommentAsync(
        Guid requestId,
        string? content,
        OperationCommentType type = OperationCommentType.Note,
        Guid? parentCommentId = null)
    {
        var trimmedContent = content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedContent))
            return (false, "Nội dung bình luận không được để trống.", Array.Empty<Guid>());
        if (trimmedContent.Length > 2000)
            return (false, "Nội dung bình luận không được vượt quá 2000 ký tự.", Array.Empty<Guid>());
        if (!Enum.IsDefined(typeof(OperationCommentType), type))
            return (false, "Loại bình luận không hợp lệ.", Array.Empty<Guid>());

        var r = await db.OperationRequests.FindAsync(requestId);
        if (r is null || r.TenantId != tenant.TenantId || r.IsDeleted)
            return (false, "Không tìm thấy yêu cầu.", Array.Empty<Guid>());
        if (!await CanSupportRequestAsync(requestId))
            return (false, "Bạn không có quyền bình luận yêu cầu này.", Array.Empty<Guid>());

        Guid? normalizedParentCommentId = null;
        if (parentCommentId.HasValue)
        {
            var parentComment = await db.OperationComments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value
                    && c.OperationRequestId == requestId
                    && c.TenantId == tenant.TenantId
                    && !c.IsDeleted);
            if (parentComment is null)
                return (false, "Không tìm thấy bình luận để trả lời.", Array.Empty<Guid>());

            normalizedParentCommentId = parentComment.ParentCommentId ?? parentComment.Id;
        }

        var mentionedUserIds = await ResolveMentionedUserIdsAsync(trimmedContent);
        var comment = new OperationComment
        {
            TenantId = tenant.TenantId,
            OperationRequestId = requestId,
            AuthorUserId = tenant.UserId,
            Type = type,
            ParentCommentId = normalizedParentCommentId,
            Content = trimmedContent,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = tenant.UserId
        };
        db.Set<OperationComment>().Add(comment);
        await audit.LogAsync("OperationRequest", requestId, "AddComment", newValueObj: new
        {
            Comment = trimmedContent,
            Type = type,
            ParentCommentId = normalizedParentCommentId,
            MentionedUserIds = mentionedUserIds
        });

        if (!await db.SaveChangesWithConcurrencyAsync())
            return (false, "Không thể thêm bình luận do dữ liệu đã thay đổi.", Array.Empty<Guid>());

        return (true, "Đã thêm bình luận.", mentionedUserIds);
    }

    private async Task<List<Guid>> ResolveMentionedUserIdsAsync(string content)
    {
        var mentionTokens = ExtractMentionTokens(content);
        if (mentionTokens.Count == 0) return [];

        var appUsers = await db.AppUsers
            .AsNoTracking()
            .Where(u => u.TenantId == tenant.TenantId
                && u.Status == UserStatus.Active
                && !u.IsDeleted
                && u.Id != tenant.UserId)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync();
        if (!appUsers.Any()) return [];

        var appUserIds = appUsers.Select(u => u.Id).ToList();
        var identityUserNames = await db.Users
            .AsNoTracking()
            .Where(u => appUserIds.Contains(u.Id))
            .Select(u => new { u.Id, UserName = u.UserName ?? string.Empty })
            .ToDictionaryAsync(u => u.Id, u => u.UserName);

        return appUsers
            .Where(u => IsMentionMatch(mentionTokens, u.FullName)
                || IsMentionMatch(mentionTokens, u.Email)
                || IsMentionMatch(mentionTokens, GetEmailLocalPart(u.Email))
                || (identityUserNames.TryGetValue(u.Id, out var userName) && IsMentionMatch(mentionTokens, userName)))
            .Select(u => u.Id)
            .Distinct()
            .ToList();
    }

    private static HashSet<string> ExtractMentionTokens(string content)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in MentionRegex.Matches(content))
        {
            var token = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(token)) continue;

            tokens.Add(token.ToLowerInvariant());
            var compactToken = ToMentionKey(token);
            if (!string.IsNullOrWhiteSpace(compactToken)) tokens.Add(compactToken);
        }

        return tokens;
    }

    private static bool IsMentionMatch(HashSet<string> mentionTokens, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        var normalized = candidate.Trim().TrimStart('@').ToLowerInvariant();
        if (mentionTokens.Contains(normalized)) return true;

        var compact = ToMentionKey(candidate);
        return !string.IsNullOrWhiteSpace(compact) && mentionTokens.Contains(compact);
    }

    private static string GetEmailLocalPart(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;

        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }

    private static string ToMentionKey(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    public async Task<(bool Success, string Message)> AddProgressAsync(OperationProgressInputViewModel vm)
    {
        var r = await db.OperationRequests.FindAsync(vm.OperationRequestId);
        if (r is null || r.TenantId != tenant.TenantId || r.IsDeleted)
            return (false, "Không tìm thấy yêu cầu.");
        if (!await CanSupportRequestAsync(r.Id))
            return (false, "Bạn không có quyền cập nhật tiến độ yêu cầu này.");
        if (r.Status != OperationStatus.InProgress)
            return (false, "Chỉ được cập nhật tiến độ khi yêu cầu đang xử lý.");
        if (vm.ProgressPercent is < 0 or > 100)
            return (false, "Tiến độ phải từ 0 đến 100%.");

        var lastProgress = await db.OperationProgressLogs
            .Where(p => p.OperationRequestId == r.Id && p.TenantId == tenant.TenantId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => (decimal?)p.ProgressPercent)
            .FirstOrDefaultAsync();

        if (lastProgress.HasValue && vm.ProgressPercent < lastProgress.Value)
            return (false, "Tiến độ mới không được nhỏ hơn lần cập nhật gần nhất.");

        var now = DateTimeOffset.UtcNow;
        var note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim();
        db.OperationProgressLogs.Add(new OperationProgressLog
        {
            TenantId = tenant.TenantId,
            OperationRequestId = r.Id,
            ProgressPercent = vm.ProgressPercent,
            Note = note,
            CreatedByUserId = tenant.UserId,
            CreatedAt = now
        });

        await audit.LogAsync("OperationRequest", r.Id, "ProgressCheckIn",
            newValueObj: new { vm.ProgressPercent, Note = note });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved
            ? (true, "Đã cập nhật tiến độ.")
            : (false, "Không thể cập nhật tiến độ do dữ liệu đã thay đổi.");
    }

    public async Task<bool> HoldAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null
            || r.TenantId != tenant.TenantId
            || !OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.OnHold)
            || !await CanManageRequestWorkAsync(id)) return false;
        var oldStatus = r.Status;
        r.Status = OperationStatus.OnHold;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        r.UpdatedByUserId = tenant.UserId;
        await audit.LogAsync("OperationRequest", id, "Hold",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = OperationStatus.OnHold });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }

    public async Task<bool> ResumeAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null
            || r.TenantId != tenant.TenantId
            || !OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.InProgress)
            || !await CanManageRequestWorkAsync(id)) return false;
        var oldStatus = r.Status;
        r.Status = OperationStatus.InProgress;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        r.UpdatedByUserId = tenant.UserId;
        await audit.LogAsync("OperationRequest", id, "Resume",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = OperationStatus.InProgress });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }

    public async Task<bool> ReopenAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null
            || r.TenantId != tenant.TenantId
            || !OperationRequestStateMachine.CanTransition(r.Status, OperationStatus.InProgress)
            || !await CanManageRequestWorkAsync(id)) return false;
        var oldStatus = r.Status;
        r.Status = OperationStatus.InProgress;
        r.UpdatedAt = DateTimeOffset.UtcNow;
        r.UpdatedByUserId = tenant.UserId;
        await audit.LogAsync("OperationRequest", id, "Reopen",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { Status = OperationStatus.InProgress });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved) await cache.InvalidateTenantCacheAsync();
        return saved;
    }
}


// ─── Work Kanban ─────────────────────────────────────────────────────────────
public class WorkKanbanService(ApplicationDbContext db, ITenantContext tenant, IAuditService audit)
{
    private sealed record WipLimitDecision(
        bool Allowed,
        bool IsExceeded,
        string? Message,
        int ProjectedCount,
        int? Limit,
        bool Enforced);

    public async Task<KanbanBoardViewModel> GetBoardAsync(
        string? search,
        Guid? departmentId,
        Guid? sprintId = null,
        Guid? assignedToUserId = null,
        PriorityLevel? priority = null,
        Guid? tagId = null,
        DateOnly? dueFrom = null,
        DateOnly? dueTo = null,
        bool hasAttachment = false,
        string? quick = null,
        Guid? savedViewId = null)
    {
        var tid = tenant.TenantId;
        if (savedViewId.HasValue)
        {
            var savedView = await db.KanbanSavedViews
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == savedViewId.Value
                    && v.TenantId == tid
                    && v.UserId == tenant.UserId
                    && !v.IsDeleted);
            if (savedView is not null)
            {
                search = savedView.SearchTerm;
                departmentId = savedView.DepartmentId;
                sprintId = savedView.SprintId;
                assignedToUserId = savedView.AssignedToUserId;
                priority = savedView.Priority;
                tagId = savedView.TagId;
                dueFrom = savedView.DueFrom;
                dueTo = savedView.DueTo;
                hasAttachment = savedView.HasAttachment;
                quick = savedView.QuickFilter;
            }
        }
        
        // 1. Retrieve dynamic columns or seed defaults if empty
        var columns = await db.KanbanColumns
            .Where(c => c.TenantId == tid && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
            
        if (!columns.Any())
        {
            var defaultCols = new List<KanbanColumn>
            {
                new() { TenantId = tid, Title = "Cần làm", AccentColor = "#8e8e93", SortOrder = 0, CreatedAt = DateTimeOffset.UtcNow },
                new() { TenantId = tid, Title = "Đang xử lý", AccentColor = "#007aff", SortOrder = 1, WipLimit = 8, CreatedAt = DateTimeOffset.UtcNow },
                new() { TenantId = tid, Title = "Đang vướng", AccentColor = "#ff9500", SortOrder = 2, CreatedAt = DateTimeOffset.UtcNow },
                new() { TenantId = tid, Title = "Hoàn thành", AccentColor = "#34c759", SortOrder = 3, IsDoneColumn = true, CreatedAt = DateTimeOffset.UtcNow },
                new() { TenantId = tid, Title = "Đã hủy", AccentColor = "#ff3b30", SortOrder = 4, IsCancelledColumn = true, CreatedAt = DateTimeOffset.UtcNow }
            };
            db.KanbanColumns.AddRange(defaultCols);
            await db.SaveChangesWithConcurrencyAsync();
            columns = defaultCols;
        }

        // 2. Query work items
        var query = db.WorkItems
            .Include(w => w.OperationRequest)
            .Include(w => w.OrganizationUnit)
            .Include(w => w.Sprint)
            .Include(w => w.Assignments)
                .ThenInclude(a => a.AssignedToUser)
            .Include(w => w.Checklists)
            .Where(w => w.TenantId == tid && !w.IsDeleted);

        if (departmentId.HasValue)
            query = query.Where(w => w.OrganizationUnitId == departmentId.Value);

        if (sprintId.HasValue)
            query = query.Where(w => w.SprintId == sprintId.Value);

        if (assignedToUserId.HasValue)
            query = query.Where(w => w.Assignments
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.AssignedAt)
                .Take(1)
                .Any(a => a.AssignedToUserId == assignedToUserId.Value));

        if (priority.HasValue)
            query = query.Where(w => w.Priority == priority.Value);

        if (dueFrom.HasValue)
            query = query.Where(w => w.DueDate >= dueFrom.Value);

        if (dueTo.HasValue)
            query = query.Where(w => w.DueDate <= dueTo.Value);

        if (hasAttachment)
            query = query.Where(w => db.Attachments.Any(a =>
                a.TenantId == tid
                && !a.IsDeleted
                && a.EntityName == "WorkItem"
                && a.EntityId == w.Id));

        if (tagId.HasValue)
            query = query.Where(w => db.EntityTags.Any(t =>
                t.TenantId == tid
                && !t.IsDeleted
                && t.EntityName == "WorkItem"
                && t.EntityId == w.Id
                && t.TagId == tagId.Value));

        var today = DateOnly.FromDateTime(DateTime.Today);
        var normalizedQuick = string.IsNullOrWhiteSpace(quick) ? null : quick.Trim().ToLowerInvariant();
        if (normalizedQuick == "mine")
        {
            query = query.Where(w => w.Assignments
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.AssignedAt)
                .Take(1)
                .Any(a => a.AssignedToUserId == tenant.UserId));
        }
        else if (normalizedQuick == "overdue")
        {
            query = query.Where(w => w.DueDate.HasValue
                && w.DueDate.Value < today
                && w.Status != WorkItemStatus.Done
                && w.Status != WorkItemStatus.Cancelled);
        }
        else if (normalizedQuick == "unassigned")
        {
            query = query.Where(w => !w.Assignments.Any(a => !a.IsDeleted));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w =>
                w.Title.Contains(term)
                || (w.Description != null && w.Description.Contains(term))
                || (w.OperationRequest != null && (w.OperationRequest.RequestNo.Contains(term) || w.OperationRequest.Title.Contains(term))));
        }

        var items = await query
            .OrderBy(w => w.DueDate ?? DateOnly.MaxValue)
            .ThenByDescending(w => w.Priority)
            .ThenByDescending(w => w.CreatedAt)
            .ToListAsync();
        var itemIds = items.Select(i => i.Id).ToList();
        var attachmentCounts = itemIds.Any()
            ? await db.Attachments
                .Where(a => a.TenantId == tid && !a.IsDeleted && a.EntityName == "WorkItem" && itemIds.Contains(a.EntityId))
                .GroupBy(a => a.EntityId)
                .Select(g => new { WorkItemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WorkItemId, x => x.Count)
            : new Dictionary<Guid, int>();
        var tagRows = await db.EntityTags
            .Include(t => t.Tag)
            .Where(t => t.TenantId == tid && !t.IsDeleted && t.EntityName == "WorkItem" && itemIds.Contains(t.EntityId) && t.Tag != null)
            .Select(t => new { WorkItemId = t.EntityId, t.Tag!.Name })
            .ToListAsync();
        var tagNames = tagRows
            .GroupBy(t => t.WorkItemId)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Name).Distinct().ToList());
        var blockingCounts = itemIds.Any()
            ? await db.WorkItemDependencies
                .Where(d => d.TenantId == tid
                    && !d.IsDeleted
                    && d.Type == WorkItemDependencyType.BlockedBy
                    && itemIds.Contains(d.BlockedId)
                    && d.Blocker != null
                    && !d.Blocker.IsDeleted
                    && d.Blocker.Status != WorkItemStatus.Done)
                .GroupBy(d => d.BlockedId)
                .Select(g => new { WorkItemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.WorkItemId, x => x.Count)
            : new Dictionary<Guid, int>();

        // 3. Auto-migrate legacy items that have KanbanColumnId == null
        var legacyItems = items.Where(w => w.KanbanColumnId == null).ToList();
        if (legacyItems.Any())
        {
            foreach (var item in legacyItems)
            {
                var matchedCol = columns.FirstOrDefault(c => 
                    item.Status == WorkItemStatus.Done && c.IsDoneColumn ||
                    item.Status == WorkItemStatus.Cancelled && c.IsCancelledColumn ||
                    item.Status == WorkItemStatus.Todo && c.SortOrder == 0 ||
                    item.Status == WorkItemStatus.Blocked && (c.Title.Contains("vướng") || c.SortOrder == 2) ||
                    item.Status == WorkItemStatus.InProgress && (c.Title.Contains("đang") || c.SortOrder == 1)
                ) ?? columns.FirstOrDefault(); // fallback
                
                if (matchedCol != null)
                {
                    item.KanbanColumnId = matchedCol.Id;
                    db.Entry(item).State = EntityState.Modified;
                }
            }
            await db.SaveChangesWithConcurrencyAsync();
        }

        // 4. Group items into board view model columns
        var boardColumns = columns.Select(c => new KanbanColumnViewModel
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description ?? "",
            AccentColor = string.IsNullOrWhiteSpace(c.AccentColor) ? "#8e8e93" : c.AccentColor,
            AccentClass = c.IsDoneColumn ? "col-done" : (c.IsCancelledColumn ? "col-cancelled" : (c.SortOrder == 0 ? "col-todo" : "col-progress")),
            SortOrder = c.SortOrder,
            IsDoneColumn = c.IsDoneColumn,
            IsCancelledColumn = c.IsCancelledColumn,
            WipLimit = c.WipLimit,
            WipEnforced = c.WipEnforced,
            Items = new List<KanbanCardViewModel>()
        }).ToList();

        var columnsMap = boardColumns.ToDictionary(c => c.Id);
        foreach (var item in items)
        {
            if (item.KanbanColumnId.HasValue && columnsMap.TryGetValue(item.KanbanColumnId.Value, out var colVm))
            {
                var checklistTotal = item.Checklists.Count(c => !c.IsDeleted);
                var checklistDone = item.Checklists.Count(c => c.IsCompleted && !c.IsDeleted);
                var checklistPercent = checklistTotal > 0 ? (int)Math.Round((decimal)checklistDone / checklistTotal * 100) : GetStatusProgress(item.Status);
                var progressPercent = checklistTotal > 0
                    ? (int)Math.Round((checklistPercent + GetStatusProgress(item.Status)) / 2m)
                    : GetStatusProgress(item.Status);
                colVm.Items.Add(new KanbanCardViewModel
                {
                    Id = item.Id,
                    OperationRequestId = item.OperationRequestId,
                    KanbanColumnId = item.KanbanColumnId.Value,
                    RequestNo = item.OperationRequest?.RequestNo ?? "",
                    RequestTitle = item.OperationRequest?.Title ?? "",
                    Title = item.Title,
                    Description = item.Description,
                    Status = item.Status,
                    IsDone = colVm.IsDoneColumn,
                    IsCancelled = colVm.IsCancelledColumn,
                    Department = item.OrganizationUnit?.Name ?? "",
                    SprintId = item.SprintId,
                    SprintName = item.Sprint?.Name,
                    Priority = item.Priority.ToString(),
                    PriorityClass = GetPriorityClass(item.Priority),
                    AssignedTo = item.Assignments
                        .OrderByDescending(a => a.AssignedAt)
                        .Select(a => a.AssignedToUser?.FullName)
                        .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    DueDate = item.DueDate,
                    ChecklistDone = checklistDone,
                    ChecklistTotal = checklistTotal,
                    ChecklistOverdueCount = item.Checklists.Count(c => !c.IsDeleted && !c.IsCompleted && c.DueDate.HasValue && c.DueDate.Value < today),
                    ProgressPercent = progressPercent,
                    AttachmentCount = attachmentCounts.GetValueOrDefault(item.Id),
                    Tags = tagNames.GetValueOrDefault(item.Id) ?? [],
                    BlockingDependencyCount = blockingCounts.GetValueOrDefault(item.Id)
                });
            }
        }

        var sprints = await GetSprintSummariesAsync(tid);
        var activeSprint = sprintId.HasValue
            ? sprints.FirstOrDefault(s => s.Id == sprintId.Value)
            : sprints.FirstOrDefault(s => s.Status == SprintStatus.Active);

        return new KanbanBoardViewModel
        {
            SearchTerm = search,
            DepartmentFilter = departmentId,
            SprintFilter = sprintId,
            AssignedToFilter = assignedToUserId,
            PriorityFilter = priority,
            TagFilter = tagId,
            DueFrom = dueFrom,
            DueTo = dueTo,
            HasAttachmentFilter = hasAttachment,
            QuickFilter = normalizedQuick,
            SavedViewId = savedViewId,
            Columns = boardColumns,
            Departments = await GetDepartmentOptionsAsync(tid),
            OperationRequests = await GetOperationRequestOptionsAsync(tid),
            SprintOptions = sprints.Select(ToSprintOption).ToList(),
            AssignableSprintOptions = sprints
                .Where(s => s.Status != SprintStatus.Closed)
                .Select(ToSprintOption)
                .ToList(),
            TagOptions = await GetTagOptionsAsync(tid),
            SavedViews = await GetSavedViewsAsync(tid),
            Assignees = await GetAssigneeOptionsAsync(tid),
            CreateForm = new WorkItemCreateViewModel { OrganizationUnitId = departmentId, SprintId = sprintId },
            ActiveSprint = activeSprint,
            Burndown = activeSprint?.Status == SprintStatus.Active ? await BuildSprintBurndownAsync(activeSprint.Id) : null,
            CanManageColumns = true
        };
    }

    public async Task<(bool Success, string Message)> CreateAsync(WorkItemCreateViewModel input)
    {
        var tid = tenant.TenantId;
        var request = await db.OperationRequests
            .FirstOrDefaultAsync(r => r.Id == input.OperationRequestId && r.TenantId == tid && !r.IsDeleted);

        if (request is null)
            return (false, "Yêu cầu vận hành không tồn tại.");

        if (request.Status is not (OperationStatus.Approved or OperationStatus.InProgress))
            return (false, "Chỉ có thể tạo thẻ công việc cho yêu cầu vận hành đã được phê duyệt hoặc đang thực thi.");

        var departmentId = input.OrganizationUnitId ?? request.OrganizationUnitId;
        var departmentExists = await db.OrganizationUnits
            .AnyAsync(o => o.Id == departmentId && o.TenantId == tid && o.IsActive && !o.IsDeleted);
        if (!departmentExists)
            return (false, "Phòng ban phụ trách không hợp lệ.");

        if (input.DueDate.HasValue && input.DueDate.Value < DateOnly.FromDateTime(DateTime.Today))
            return (false, "Hạn xử lý không được nhỏ hơn ngày hôm nay.");

        var sprint = await GetAssignableSprintAsync(input.SprintId);
        if (input.SprintId.HasValue && sprint is null)
            return (false, "Sprint không hợp lệ hoặc đã đóng.");

        AppUser? assignee = null;
        if (input.AssignedToUserId.HasValue)
        {
            assignee = await db.AppUsers
                .FirstOrDefaultAsync(u => u.Id == input.AssignedToUserId.Value && u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted);
            if (assignee is null)
                return (false, "Người được giao không hợp lệ.");
        }

        // Get first dynamic column
        var firstColumn = await db.KanbanColumns
            .Where(c => c.TenantId == tid && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .FirstOrDefaultAsync();
        WipLimitDecision? wipDecision = null;
        if (firstColumn is not null)
        {
            wipDecision = await EvaluateWipLimitAsync(firstColumn);
            if (!wipDecision.Allowed)
                return (false, wipDecision.Message ?? "Cột Kanban đã vượt giới hạn WIP.");
        }

        var workItem = new WorkItem
        {
            TenantId = tid,
            OperationRequestId = request.Id,
            OrganizationUnitId = departmentId,
            Title = input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Priority = input.Priority,
            Status = WorkItemStatus.Todo,
            KanbanColumnId = firstColumn?.Id,
            SprintId = sprint?.Id,
            DueDate = input.DueDate,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.WorkItems.Add(workItem);

        if (assignee is not null)
        {
            db.WorkItemAssignments.Add(new WorkItemAssignment
            {
                TenantId = tid,
                WorkItemId = workItem.Id,
                AssignedToUserId = assignee.Id,
                AssignedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = tenant.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (request.Status != OperationStatus.InProgress && OperationRequestStateMachine.CanTransition(request.Status, OperationStatus.InProgress))
        {
            request.Status = OperationStatus.InProgress;
            request.UpdatedAt = DateTimeOffset.UtcNow;
            request.UpdatedByUserId = tenant.UserId;
        }

        await audit.LogAsync("WorkItem", workItem.Id, "Create",
            newValueObj: new { workItem.Title, workItem.Status, workItem.OperationRequestId, workItem.SprintId, AssignedToUserId = assignee?.Id, WipWarning = wipDecision?.Message });
        if (wipDecision?.IsExceeded == true && firstColumn is not null)
        {
            await audit.LogAsync("KanbanColumn", firstColumn.Id, "WipLimitExceeded",
                newValueObj: new { firstColumn.Title, wipDecision.ProjectedCount, wipDecision.Limit, wipDecision.Enforced, WorkItemId = workItem.Id });
        }

        var saveResult = await db.SaveChangesWithConcurrencyMessageAsync("Đã tạo thẻ công việc trên Kanban.");
        return saveResult.Success && wipDecision?.IsExceeded == true
            ? (true, wipDecision.Message ?? "Đã tạo thẻ nhưng cột đã vượt giới hạn WIP.")
            : saveResult;
    }

    public async Task<(bool Success, string Message)> MoveAsync(Guid workItemId, WorkItemStatus newStatus)
    {
        var tid = tenant.TenantId;
        var matchedCol = await db.KanbanColumns
            .Where(c => c.TenantId == tid && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var targetCol = matchedCol.FirstOrDefault(c => 
            newStatus == WorkItemStatus.Done && c.IsDoneColumn ||
            newStatus == WorkItemStatus.Cancelled && c.IsCancelledColumn ||
            newStatus == WorkItemStatus.Todo && c.SortOrder == 0 ||
            newStatus == WorkItemStatus.Blocked && (c.Title.Contains("vướng") || c.SortOrder == 2) ||
            newStatus == WorkItemStatus.InProgress && (c.Title.Contains("đang") || c.SortOrder == 1)
        ) ?? matchedCol.FirstOrDefault();

        if (targetCol == null)
            return (false, "Không tìm thấy cột Kanban tương ứng.");

        return await MoveToColumnAsync(workItemId, targetCol.Id);
    }

    public async Task<(bool Success, string Message)> MoveToColumnAsync(Guid workItemId, Guid targetColumnId)
    {
        var tid = tenant.TenantId;
        var item = await db.WorkItems
            .Include(w => w.OperationRequest)
            .Include(w => w.Assignments)
            .FirstOrDefaultAsync(w => w.Id == workItemId && w.TenantId == tid && !w.IsDeleted);

        if (item is null)
            return (false, "Không tìm thấy công việc.");

        var col = await db.KanbanColumns
            .FirstOrDefaultAsync(c => c.Id == targetColumnId && c.TenantId == tid && !c.IsDeleted);
        if (col is null)
            return (false, "Không tìm thấy cột mục tiêu.");

        if (item.KanbanColumnId == targetColumnId)
            return (true, "Thẻ công việc đã ở trong cột này.");

        var wipDecision = await EvaluateWipLimitAsync(col, item.Id);
        if (!wipDecision.Allowed)
            return (false, wipDecision.Message ?? "Cột mục tiêu đã vượt giới hạn WIP.");

        var oldStatus = item.Status;
        var newStatus = ResolveStatusForColumn(col);
        if (newStatus != oldStatus && !WorkItemStateMachine.CanTransition(oldStatus, newStatus))
            return (false, $"Không thể chuyển công việc từ {oldStatus} sang {newStatus}.");
        var dependencyWarning = await GetDependencyWarningAsync(item.Id, newStatus);

        var oldColumnId = item.KanbanColumnId;
        item.KanbanColumnId = targetColumnId;
        item.Status = newStatus;

        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = tenant.UserId;
        db.WorkItemActivities.Add(new WorkItemActivity
        {
            TenantId = tid,
            WorkItemId = item.Id,
            FromColumnId = oldColumnId,
            ToColumnId = targetColumnId,
            MovedAt = item.UpdatedAt.Value,
            MovedByUserId = tenant.UserId,
            CreatedByUserId = tenant.UserId,
            CreatedAt = item.UpdatedAt.Value
        });

        foreach (var assignment in item.Assignments.Where(a => !a.IsDeleted))
        {
            assignment.CompletedAt = item.Status == WorkItemStatus.Done ? DateTimeOffset.UtcNow : null;
            assignment.UpdatedAt = DateTimeOffset.UtcNow;
            assignment.UpdatedByUserId = tenant.UserId;
        }

        await SyncOperationStatusAsync(item);

        await audit.LogAsync("WorkItem", item.Id, "MoveKanbanCard",
            oldValueObj: new { KanbanColumnId = oldColumnId, Status = oldStatus },
            newValueObj: new { KanbanColumnId = targetColumnId, item.Status, WipWarning = wipDecision.Message, DependencyWarning = dependencyWarning });
        if (wipDecision.IsExceeded)
        {
            await audit.LogAsync("KanbanColumn", col.Id, "WipLimitExceeded",
                newValueObj: new { col.Title, wipDecision.ProjectedCount, wipDecision.Limit, wipDecision.Enforced, WorkItemId = item.Id });
        }

        var saveResult = await db.SaveChangesWithConcurrencyMessageAsync("Đã di chuyển trạng thái Kanban.");
        if (!saveResult.Success)
            return saveResult;

        var finalMessage = wipDecision.IsExceeded
            ? wipDecision.Message ?? "Đã di chuyển thẻ nhưng cột đã vượt giới hạn WIP."
            : saveResult.Message;
        if (!string.IsNullOrWhiteSpace(dependencyWarning))
            finalMessage = $"{finalMessage} {dependencyWarning}";
        return (true, finalMessage);
    }

    // ── Column Management ──────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> CreateColumnAsync(string title, string? accentColor, int? wipLimit = null, bool wipEnforced = false)
    {
        var tid = tenant.TenantId;
        if (wipLimit.HasValue && wipLimit.Value <= 0)
            return (false, "WIP limit phải lớn hơn 0.");

        var maxSort = await db.KanbanColumns
            .Where(c => c.TenantId == tid && !c.IsDeleted)
            .MaxAsync(c => (int?)c.SortOrder) ?? -1;

        var col = new KanbanColumn
        {
            TenantId = tid,
            Title = title.Trim(),
            AccentColor = string.IsNullOrWhiteSpace(accentColor) ? "#8e8e93" : accentColor.Trim(),
            SortOrder = maxSort + 1,
            WipLimit = wipLimit,
            WipEnforced = wipLimit.HasValue && wipEnforced,
            CreatedByUserId = tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.KanbanColumns.Add(col);
        await db.SaveChangesAsync();
        return (true, "Đã tạo cột mới.");
    }

    public async Task<(bool Success, string Message)> UpdateColumnWipLimitAsync(Guid columnId, int? wipLimit, bool wipEnforced)
    {
        if (wipLimit.HasValue && wipLimit.Value <= 0)
            return (false, "WIP limit phải lớn hơn 0.");

        var col = await db.KanbanColumns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (col is null) return (false, "Không tìm thấy cột.");

        var oldValue = new { col.WipLimit, col.WipEnforced };
        col.WipLimit = wipLimit;
        col.WipEnforced = wipLimit.HasValue && wipEnforced;
        col.UpdatedAt = DateTimeOffset.UtcNow;
        col.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync("KanbanColumn", col.Id, "UpdateWipLimit",
            oldValueObj: oldValue,
            newValueObj: new { col.WipLimit, col.WipEnforced });

        return await db.SaveChangesWithConcurrencyMessageAsync("Đã cập nhật WIP limit.");
    }

    public async Task<(bool Success, string Message)> RenameColumnAsync(Guid columnId, string title)
    {
        var col = await db.KanbanColumns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (col == null) return (false, "Không tìm thấy cột.");

        col.Title = title.Trim();
        col.UpdatedAt = DateTimeOffset.UtcNow;
        col.UpdatedByUserId = tenant.UserId;

        await db.SaveChangesAsync();
        return (true, "Đã đổi tên cột.");
    }

    public async Task<(bool Success, string Message)> DeleteColumnAsync(Guid columnId)
    {
        var tid = tenant.TenantId;
        var col = await db.KanbanColumns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.TenantId == tid && !c.IsDeleted);
        if (col == null) return (false, "Không tìm thấy cột.");

        var remainingCols = await db.KanbanColumns
            .Where(c => c.TenantId == tid && c.Id != columnId && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        if (!remainingCols.Any())
            return (false, "Không thể xóa cột duy nhất còn lại trên bảng.");

        var fallbackCol = remainingCols.FirstOrDefault();

        var cards = await db.WorkItems
            .Where(w => w.TenantId == tid && w.KanbanColumnId == columnId && !w.IsDeleted)
            .ToListAsync();

        foreach (var card in cards)
        {
            card.KanbanColumnId = fallbackCol?.Id;
            if (fallbackCol != null)
            {
                var fallbackStatus = ResolveStatusForColumn(fallbackCol);
                if (card.Status == fallbackStatus || WorkItemStateMachine.CanTransition(card.Status, fallbackStatus))
                    card.Status = fallbackStatus;
            }
        }

        col.IsDeleted = true;
        col.UpdatedAt = DateTimeOffset.UtcNow;
        col.UpdatedByUserId = tenant.UserId;

        for (int i = 0; i < remainingCols.Count; i++)
        {
            remainingCols[i].SortOrder = i;
            db.Entry(remainingCols[i]).State = EntityState.Modified;
        }

        return await db.SaveChangesWithConcurrencyMessageAsync($"Đã xóa cột \"{col.Title}\". Thẻ trong cột được chuyển sang \"{fallbackCol?.Title}\".");
    }

    public async Task<(bool Success, string Message)> MoveColumnAsync(Guid columnId, string direction)
    {
        var tid = tenant.TenantId;
        var cols = await db.KanbanColumns
            .Where(c => c.TenantId == tid && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var currentIdx = cols.FindIndex(c => c.Id == columnId);
        if (currentIdx == -1) return (false, "Không tìm thấy cột.");

        int targetIdx = -1;
        if (direction == "left" && currentIdx > 0)
        {
            targetIdx = currentIdx - 1;
        }
        else if (direction == "right" && currentIdx < cols.Count - 1)
        {
            targetIdx = currentIdx + 1;
        }

        if (targetIdx == -1) return (false, "Không thể di chuyển cột theo hướng này.");

        var temp = cols[currentIdx].SortOrder;
        cols[currentIdx].SortOrder = cols[targetIdx].SortOrder;
        cols[targetIdx].SortOrder = temp;

        await db.SaveChangesAsync();
        return (true, "Đã di chuyển vị trí cột.");
    }

    private async Task SyncOperationStatusAsync(WorkItem item)
    {
        var request = item.OperationRequest;
        if (request is null || request.Status is OperationStatus.Draft or OperationStatus.Submitted or OperationStatus.InReview or OperationStatus.Rejected or OperationStatus.Cancelled)
            return;

        var remainingActiveItems = await db.WorkItems.CountAsync(w =>
            w.OperationRequestId == item.OperationRequestId
            && w.Id != item.Id
            && !w.IsDeleted
            && w.Status != WorkItemStatus.Done
            && w.Status != WorkItemStatus.Cancelled);

        var nextRequestStatus = item.Status == WorkItemStatus.Done && remainingActiveItems == 0
            ? OperationStatus.Completed
            : OperationStatus.InProgress;

        if (request.Status == nextRequestStatus)
            return;

        if (!OperationRequestStateMachine.CanTransition(request.Status, nextRequestStatus))
            return;

        request.Status = nextRequestStatus;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedByUserId = tenant.UserId;
    }

    private async Task<string?> GetDependencyWarningAsync(Guid workItemId, WorkItemStatus targetStatus)
    {
        if (targetStatus != WorkItemStatus.InProgress)
            return null;

        var blockerQuery = db.WorkItemDependencies
            .Where(d => d.TenantId == tenant.TenantId
                && !d.IsDeleted
                && d.BlockedId == workItemId
                && d.Type == WorkItemDependencyType.BlockedBy
                && d.Blocker != null
                && !d.Blocker.IsDeleted
                && d.Blocker.Status != WorkItemStatus.Done);
        var blockerCount = await blockerQuery.CountAsync();
        var blockers = await blockerQuery
            .OrderBy(d => d.Blocker!.DueDate ?? DateOnly.MaxValue)
            .Select(d => d.Blocker!.Title)
            .Take(3)
            .ToListAsync();

        if (blockerCount == 0)
            return null;

        return $"Cảnh báo dependency: còn {blockerCount} blocker chưa Done ({string.Join(", ", blockers)}).";
    }

    private async Task<WipLimitDecision> EvaluateWipLimitAsync(KanbanColumn column, Guid? movingWorkItemId = null)
    {
        if (!column.WipLimit.HasValue)
            return new(true, false, null, 0, null, false);

        var currentCount = await db.WorkItems.CountAsync(w =>
            w.TenantId == tenant.TenantId
            && w.KanbanColumnId == column.Id
            && !w.IsDeleted
            && (!movingWorkItemId.HasValue || w.Id != movingWorkItemId.Value));
        var projectedCount = currentCount + 1;
        if (projectedCount <= column.WipLimit.Value)
            return new(true, false, null, projectedCount, column.WipLimit, column.WipEnforced);

        var message = column.WipEnforced
            ? $"Cột \"{column.Title}\" đã đạt WIP limit {currentCount}/{column.WipLimit}. Không thể thêm thẻ."
            : $"Cảnh báo WIP: cột \"{column.Title}\" sẽ vượt {projectedCount}/{column.WipLimit} thẻ.";
        return new(!column.WipEnforced, true, message, projectedCount, column.WipLimit, column.WipEnforced);
    }

    private static WorkItemStatus ResolveStatusForColumn(KanbanColumn col)
    {
        if (col.IsDoneColumn) return WorkItemStatus.Done;
        if (col.IsCancelledColumn) return WorkItemStatus.Cancelled;
        if (col.SortOrder == 0) return WorkItemStatus.Todo;
        if (col.SortOrder == 2 || col.Title.Contains("vướng", StringComparison.OrdinalIgnoreCase)) return WorkItemStatus.Blocked;
        return WorkItemStatus.InProgress;
    }

    private static string GetPriorityClass(PriorityLevel priority) => priority switch
    {
        PriorityLevel.Low => "priority-low",
        PriorityLevel.Normal => "priority-normal",
        PriorityLevel.High => "priority-high",
        PriorityLevel.Critical => "priority-critical",
        _ => "priority-normal"
    };

    private static int GetStatusProgress(WorkItemStatus status) => status switch
    {
        WorkItemStatus.Todo => 0,
        WorkItemStatus.InProgress => 50,
        WorkItemStatus.Blocked => 35,
        WorkItemStatus.Done => 100,
        WorkItemStatus.Cancelled => 0,
        _ => 0
    };

    private Task<List<SelectOption>> GetDepartmentOptionsAsync(Guid tenantId) =>
        db.OrganizationUnits
            .Where(o => o.TenantId == tenantId && o.IsActive && !o.IsDeleted)
            .OrderBy(o => o.Name)
            .Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name })
            .ToListAsync();

    private Task<List<SelectOption>> GetAssigneeOptionsAsync(Guid tenantId) =>
        db.AppUsers
            .Where(u => u.TenantId == tenantId && u.Status == UserStatus.Active && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();

    private Task<List<SelectOption>> GetOperationRequestOptionsAsync(Guid tenantId) =>
        db.OperationRequests
            .Where(r => r.TenantId == tenantId && !r.IsDeleted && r.Status != OperationStatus.Rejected && r.Status != OperationStatus.Cancelled)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new SelectOption { Value = r.Id.ToString(), Text = r.RequestNo + " - " + r.Title })
            .ToListAsync();

    private Task<List<SelectOption>> GetTagOptionsAsync(Guid tenantId) =>
        db.Tags
            .Where(t => t.TenantId == tenantId && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .Select(t => new SelectOption { Value = t.Id.ToString(), Text = t.Name })
            .ToListAsync();

    private Task<List<KanbanSavedViewItem>> GetSavedViewsAsync(Guid tenantId) =>
        db.KanbanSavedViews
            .Where(v => v.TenantId == tenantId && v.UserId == tenant.UserId && !v.IsDeleted)
            .OrderBy(v => v.Name)
            .Select(v => new KanbanSavedViewItem { Id = v.Id, Name = v.Name })
            .ToListAsync();

    public async Task<(bool Success, string Message)> SaveKanbanViewAsync(
        string name,
        string? search,
        Guid? departmentId,
        Guid? sprintId,
        Guid? assignedToUserId,
        PriorityLevel? priority,
        Guid? tagId,
        DateOnly? dueFrom,
        DateOnly? dueTo,
        bool hasAttachment,
        string? quick)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Tên saved view không được để trống.");

        var normalizedName = name.Trim();
        var existing = await db.KanbanSavedViews.FirstOrDefaultAsync(v =>
            v.TenantId == tenant.TenantId
            && v.UserId == tenant.UserId
            && !v.IsDeleted
            && v.Name == normalizedName);
        if (existing is null)
        {
            existing = new KanbanSavedView
            {
                TenantId = tenant.TenantId,
                UserId = tenant.UserId,
                Name = normalizedName,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = tenant.UserId
            };
            db.KanbanSavedViews.Add(existing);
        }

        existing.SearchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        existing.DepartmentId = departmentId;
        existing.SprintId = sprintId;
        existing.AssignedToUserId = assignedToUserId;
        existing.Priority = priority;
        existing.TagId = tagId;
        existing.DueFrom = dueFrom;
        existing.DueTo = dueTo;
        existing.HasAttachment = hasAttachment;
        existing.QuickFilter = string.IsNullOrWhiteSpace(quick) ? null : quick.Trim().ToLowerInvariant();
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync("KanbanSavedView", existing.Id, "Save",
            newValueObj: new { existing.Name, existing.SearchTerm, existing.DepartmentId, existing.SprintId, existing.AssignedToUserId, existing.Priority, existing.TagId, existing.DueFrom, existing.DueTo, existing.HasAttachment, existing.QuickFilter });

        return await db.SaveChangesWithConcurrencyMessageAsync("Đã lưu saved view Kanban.");
    }

    public async Task<(bool Success, string Message)> DeleteKanbanViewAsync(Guid id)
    {
        var view = await db.KanbanSavedViews
            .FirstOrDefaultAsync(v => v.Id == id
                && v.TenantId == tenant.TenantId
                && v.UserId == tenant.UserId
                && !v.IsDeleted);
        if (view is null)
            return (false, "Không tìm thấy saved view.");

        view.IsDeleted = true;
        view.UpdatedAt = DateTimeOffset.UtcNow;
        view.UpdatedByUserId = tenant.UserId;
        await audit.LogAsync("KanbanSavedView", view.Id, "Delete", oldValueObj: new { view.Name });

        return await db.SaveChangesWithConcurrencyMessageAsync("Đã xóa saved view.");
    }

    private async Task<List<SprintSummaryViewModel>> GetSprintSummariesAsync(Guid tenantId) =>
        await db.Sprints
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .OrderByDescending(s => s.Status == SprintStatus.Active)
            .ThenByDescending(s => s.StartDate)
            .Select(s => new SprintSummaryViewModel
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Goal = s.Goal,
                Status = s.Status,
                TotalItems = s.WorkItems.Count(w => !w.IsDeleted),
                DoneItems = s.WorkItems.Count(w => !w.IsDeleted && w.Status == WorkItemStatus.Done)
            })
            .ToListAsync();

    private static SelectOption ToSprintOption(SprintSummaryViewModel sprint) => new()
    {
        Value = sprint.Id.ToString(),
        Text = $"{sprint.Name} ({GetSprintStatusLabel(sprint.Status)})"
    };

    private Task<Sprint?> GetAssignableSprintAsync(Guid? sprintId)
    {
        if (!sprintId.HasValue)
            return Task.FromResult<Sprint?>(null);

        return db.Sprints.FirstOrDefaultAsync(s =>
            s.Id == sprintId.Value
            && s.TenantId == tenant.TenantId
            && !s.IsDeleted
            && s.Status != SprintStatus.Closed);
    }

    public async Task<(bool Success, string Message, Guid? SprintId)> CreateSprintAsync(WorkflowSprintCreateViewModel input)
    {
        var tid = tenant.TenantId;
        if (string.IsNullOrWhiteSpace(input.Name))
            return (false, "Tên sprint không được để trống.", null);

        if (input.StartDate > input.EndDate)
            return (false, "Ngày kết thúc sprint phải lớn hơn hoặc bằng ngày bắt đầu.", null);

        if (input.Status == SprintStatus.Active)
        {
            var hasActiveSprint = await db.Sprints.AnyAsync(s =>
                s.TenantId == tid
                && !s.IsDeleted
                && s.Status == SprintStatus.Active);
            if (hasActiveSprint)
                return (false, "Đã có sprint đang Active. Hãy đóng sprint hiện tại trước khi mở sprint mới.", null);
        }

        var sprint = new Sprint
        {
            TenantId = tid,
            Name = input.Name.Trim(),
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            Goal = string.IsNullOrWhiteSpace(input.Goal) ? null : input.Goal.Trim(),
            Status = input.Status,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = tenant.UserId
        };

        db.Sprints.Add(sprint);
        await audit.LogAsync("Sprint", sprint.Id, "Create",
            newValueObj: new { sprint.Name, sprint.StartDate, sprint.EndDate, sprint.Goal, sprint.Status });

        var saveResult = await db.SaveChangesWithConcurrencyMessageAsync("Đã tạo sprint.");
        return (saveResult.Success, saveResult.Message, saveResult.Success ? sprint.Id : null);
    }

    public async Task<(bool Success, string Message)> UpdateSprintStatusAsync(Guid sprintId, SprintStatus status)
    {
        var sprint = await db.Sprints
            .FirstOrDefaultAsync(s => s.Id == sprintId && s.TenantId == tenant.TenantId && !s.IsDeleted);
        if (sprint is null)
            return (false, "Không tìm thấy sprint.");

        if (sprint.Status == status)
            return (true, "Sprint đã ở trạng thái này.");

        if (sprint.Status == SprintStatus.Closed)
            return (false, "Sprint đã đóng không thể đổi trạng thái.");

        var allowed = sprint.Status switch
        {
            SprintStatus.Planned => status is SprintStatus.Active or SprintStatus.Closed,
            SprintStatus.Active => status == SprintStatus.Closed,
            _ => false
        };
        if (!allowed)
            return (false, $"Không thể chuyển sprint từ {GetSprintStatusLabel(sprint.Status)} sang {GetSprintStatusLabel(status)}.");

        if (status == SprintStatus.Active)
        {
            var hasActiveSprint = await db.Sprints.AnyAsync(s =>
                s.Id != sprint.Id
                && s.TenantId == tenant.TenantId
                && !s.IsDeleted
                && s.Status == SprintStatus.Active);
            if (hasActiveSprint)
                return (false, "Đã có sprint đang Active. Hãy đóng sprint hiện tại trước.");
        }

        var oldStatus = sprint.Status;
        sprint.Status = status;
        sprint.UpdatedAt = DateTimeOffset.UtcNow;
        sprint.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync("Sprint", sprint.Id, "UpdateStatus",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { sprint.Status });

        return await db.SaveChangesWithConcurrencyMessageAsync("Đã cập nhật trạng thái sprint.");
    }

    private async Task<SprintBurndownViewModel?> BuildSprintBurndownAsync(Guid sprintId)
    {
        var tid = tenant.TenantId;
        var sprint = await db.Sprints
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sprintId && s.TenantId == tid && !s.IsDeleted);
        if (sprint is null)
            return null;

        var items = await db.WorkItems
            .AsNoTracking()
            .Where(w => w.TenantId == tid && w.SprintId == sprint.Id && !w.IsDeleted)
            .Select(w => new { w.Id, w.Status, w.UpdatedAt, w.CreatedAt })
            .ToListAsync();
        var itemIds = items.Select(i => i.Id).ToList();

        var doneColumnIds = await db.KanbanColumns
            .AsNoTracking()
            .Where(c => c.TenantId == tid && !c.IsDeleted && c.IsDoneColumn)
            .Select(c => c.Id)
            .ToListAsync();

        var doneMoves = itemIds.Any() && doneColumnIds.Any()
            ? await db.WorkItemActivities
                .AsNoTracking()
                .Where(a => a.TenantId == tid
                    && !a.IsDeleted
                    && itemIds.Contains(a.WorkItemId)
                    && a.ToColumnId.HasValue
                    && doneColumnIds.Contains(a.ToColumnId.Value))
                .GroupBy(a => a.WorkItemId)
                .Select(g => new { WorkItemId = g.Key, CompletedAt = g.Min(a => a.MovedAt) })
                .ToListAsync()
            : [];
        var completedAtByItem = doneMoves.ToDictionary(x => x.WorkItemId, x => x.CompletedAt);

        var totalScope = items.Count;
        var doneCount = items.Count(i => i.Status == WorkItemStatus.Done);
        var days = Math.Max(1, sprint.EndDate.DayNumber - sprint.StartDate.DayNumber + 1);
        var points = new List<SprintBurndownPoint>();

        for (var index = 0; index < days; index++)
        {
            var date = sprint.StartDate.AddDays(index);
            var dayEnd = new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            var completedByDay = items.Count(item =>
            {
                if (item.Status != WorkItemStatus.Done)
                    return false;

                var completedAt = completedAtByItem.TryGetValue(item.Id, out var movedAt)
                    ? movedAt
                    : item.UpdatedAt ?? item.CreatedAt;
                return completedAt <= dayEnd;
            });

            var progress = days == 1 ? 1d : (double)index / (days - 1);
            var idealRemaining = Math.Max(0, (int)Math.Round(totalScope * (1 - progress), MidpointRounding.AwayFromZero));
            points.Add(new SprintBurndownPoint
            {
                DateLabel = date.ToString("dd/MM"),
                IdealRemaining = idealRemaining,
                ActualRemaining = Math.Max(0, totalScope - completedByDay)
            });
        }

        return new SprintBurndownViewModel
        {
            SprintId = sprint.Id,
            SprintName = sprint.Name,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            TotalScope = totalScope,
            DoneCount = doneCount,
            Points = points
        };
    }

    public async Task<WorkflowAnalyticsViewModel> GetAnalyticsAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var tid = tenant.TenantId;
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        var fromDate = from ?? toDate.AddDays(-30);
        if (fromDate > toDate) (fromDate, toDate) = (toDate, fromDate);

        var fromAt = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toAt = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        var now = DateTimeOffset.UtcNow;

        var columns = await db.KanbanColumns
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.TenantId == tid)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Title, c.AccentColor, c.SortOrder, c.IsDoneColumn, c.IsCancelledColumn })
            .ToListAsync();
        var columnMap = columns.ToDictionary(c => c.Id);
        var doneColumnIds = columns.Where(c => c.IsDoneColumn).Select(c => c.Id).ToHashSet();

        var workItems = await db.WorkItems
            .AsNoTracking()
            .Where(w => w.TenantId == tid && !w.IsDeleted)
            .Select(w => new { w.Id, w.CreatedAt, w.UpdatedAt, w.Status, w.KanbanColumnId })
            .ToListAsync();
        var workItemIds = workItems.Select(w => w.Id).ToList();
        var activities = workItemIds.Any()
            ? await db.WorkItemActivities
                .AsNoTracking()
                .Where(a => a.TenantId == tid && workItemIds.Contains(a.WorkItemId) && !a.IsDeleted)
                .OrderBy(a => a.MovedAt)
                .Select(a => new { a.WorkItemId, a.FromColumnId, a.ToColumnId, a.MovedAt })
                .ToListAsync()
            : [];
        var activitiesByItem = activities
            .GroupBy(a => a.WorkItemId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.MovedAt).ToList());

        var completedMeasurements = workItems
            .Where(w => w.Status == WorkItemStatus.Done)
            .Select(w =>
            {
                activitiesByItem.TryGetValue(w.Id, out var itemActivities);
                itemActivities ??= [];
                var completedAt = itemActivities.FirstOrDefault(a => a.ToColumnId.HasValue && doneColumnIds.Contains(a.ToColumnId.Value))?.MovedAt
                    ?? w.UpdatedAt
                    ?? w.CreatedAt;
                var firstInProgressAt = itemActivities.FirstOrDefault(a => a.ToColumnId.HasValue && ResolveColumnStatus(a.ToColumnId.Value) == WorkItemStatus.InProgress)?.MovedAt
                    ?? w.CreatedAt;
                return new
                {
                    w.Id,
                    CompletedAt = completedAt,
                    LeadDays = Math.Max(0, (completedAt - w.CreatedAt).TotalDays),
                    CycleDays = Math.Max(0, (completedAt - firstInProgressAt).TotalDays)
                };
            })
            .ToList();

        var cycleTrend = completedMeasurements
            .Where(x => x.CompletedAt >= fromAt && x.CompletedAt <= toAt)
            .GroupBy(x => StartOfWeek(DateOnly.FromDateTime(x.CompletedAt.DateTime)))
            .OrderBy(g => g.Key)
            .Select(g => new CycleTimeTrendItem
            {
                PeriodLabel = $"{g.Key:dd/MM}",
                AverageCycleDays = Math.Round(g.Average(x => x.CycleDays), 2),
                CompletedCount = g.Count()
            })
            .ToList();
        var completedInRange = completedMeasurements
            .Where(x => x.CompletedAt >= fromAt && x.CompletedAt <= toAt)
            .ToList();

        var columnDurations = new Dictionary<Guid, (double Hours, int Visits)>();
        var unassignedDuration = (Hours: 0d, Visits: 0);
        foreach (var item in workItems)
        {
            activitiesByItem.TryGetValue(item.Id, out var itemActivities);
            itemActivities ??= [];

            if (!itemActivities.Any())
            {
                AddColumnDuration(item.KanbanColumnId, item.CreatedAt, item.Status == WorkItemStatus.Done ? item.UpdatedAt ?? now : now);
                continue;
            }

            AddColumnDuration(itemActivities[0].FromColumnId ?? item.KanbanColumnId, item.CreatedAt, itemActivities[0].MovedAt);
            for (var i = 0; i < itemActivities.Count; i++)
            {
                var start = itemActivities[i].MovedAt;
                var end = i < itemActivities.Count - 1
                    ? itemActivities[i + 1].MovedAt
                    : ResolveNullableColumnStatus(itemActivities[i].ToColumnId) is WorkItemStatus.Done or WorkItemStatus.Cancelled ? itemActivities[i].MovedAt : now;
                AddColumnDuration(itemActivities[i].ToColumnId, start, end);
            }
        }

        var columnTimes = columnDurations
            .Select(kvp =>
            {
                var title = columnMap.TryGetValue(kvp.Key, out var col)
                    ? col.Title
                    : "Không rõ cột";
                var accent = columnMap.TryGetValue(kvp.Key, out col)
                    ? col.AccentColor
                    : "#8e8e93";
                return new ColumnTimeAnalyticsItem
                {
                    ColumnId = kvp.Key,
                    ColumnTitle = title,
                    AccentColor = string.IsNullOrWhiteSpace(accent) ? "#8e8e93" : accent,
                    AverageHours = kvp.Value.Visits > 0 ? Math.Round(kvp.Value.Hours / kvp.Value.Visits, 1) : 0,
                    VisitCount = kvp.Value.Visits
                };
            })
            .ToList();
        if (unassignedDuration.Visits > 0)
        {
            columnTimes.Add(new ColumnTimeAnalyticsItem
            {
                ColumnId = null,
                ColumnTitle = "Chưa phân cột",
                AccentColor = "#8e8e93",
                AverageHours = Math.Round(unassignedDuration.Hours / unassignedDuration.Visits, 1),
                VisitCount = unassignedDuration.Visits
            });
        }
        columnTimes = columnTimes.OrderByDescending(c => c.AverageHours).ToList();
        if (columnTimes.Any()) columnTimes[0].IsBottleneck = true;

        return new WorkflowAnalyticsViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalCards = workItems.Count,
            CompletedCards = completedInRange.Count,
            MovedCards = activities.Count(a => a.MovedAt >= fromAt && a.MovedAt <= toAt),
            AverageLeadDays = completedInRange.Any() ? Math.Round(completedInRange.Average(x => x.LeadDays), 2) : 0,
            AverageCycleDays = completedInRange.Any() ? Math.Round(completedInRange.Average(x => x.CycleDays), 2) : 0,
            CycleTimeTrend = cycleTrend,
            ColumnTimes = columnTimes,
            CumulativeFlow = BuildCumulativeFlow()
        };

        void AddColumnDuration(Guid? columnId, DateTimeOffset start, DateTimeOffset end)
        {
            var clippedStart = start > fromAt ? start : fromAt;
            var clippedEnd = end < toAt ? end : toAt;
            if (clippedEnd <= clippedStart) return;

            var hours = (clippedEnd - clippedStart).TotalHours;
            if (!columnId.HasValue)
            {
                unassignedDuration = (unassignedDuration.Hours + hours, unassignedDuration.Visits + 1);
                return;
            }

            var current = columnDurations.TryGetValue(columnId.Value, out var value) ? value : (Hours: 0d, Visits: 0);
            columnDurations[columnId.Value] = (current.Hours + hours, current.Visits + 1);
        }

        WorkItemStatus ResolveNullableColumnStatus(Guid? columnId)
        {
            if (!columnId.HasValue) return WorkItemStatus.InProgress;
            return ResolveColumnStatus(columnId.Value);
        }

        WorkItemStatus ResolveColumnStatus(Guid columnId)
        {
            if (!columnMap.TryGetValue(columnId, out var column)) return WorkItemStatus.InProgress;
            if (column.IsDoneColumn) return WorkItemStatus.Done;
            if (column.IsCancelledColumn) return WorkItemStatus.Cancelled;
            if (column.SortOrder == 0) return WorkItemStatus.Todo;
            if (column.SortOrder == 2 || column.Title.Contains("vướng", StringComparison.OrdinalIgnoreCase)) return WorkItemStatus.Blocked;
            return WorkItemStatus.InProgress;
        }

        List<CumulativeFlowPoint> BuildCumulativeFlow()
        {
            var points = new List<CumulativeFlowPoint>();
            var maxDays = Math.Min(45, toDate.DayNumber - fromDate.DayNumber + 1);
            var startDate = toDate.AddDays(-(maxDays - 1));
            if (startDate < fromDate) startDate = fromDate;

            for (var date = startDate; date <= toDate; date = date.AddDays(1))
            {
                var dayEnd = new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
                var counts = columns.ToDictionary(c => c.Id, _ => 0);

                foreach (var item in workItems.Where(w => w.CreatedAt <= dayEnd))
                {
                    activitiesByItem.TryGetValue(item.Id, out var itemActivities);
                    itemActivities ??= [];
                    var latestMove = itemActivities.LastOrDefault(a => a.MovedAt <= dayEnd);
                    var columnId = latestMove?.ToColumnId
                        ?? itemActivities.FirstOrDefault()?.FromColumnId
                        ?? item.KanbanColumnId;
                    if (columnId.HasValue && counts.ContainsKey(columnId.Value))
                        counts[columnId.Value]++;
                }

                points.Add(new CumulativeFlowPoint
                {
                    DateLabel = date.ToString("dd/MM"),
                    Columns = columns.Select(c => new ColumnCountPoint
                    {
                        ColumnTitle = c.Title,
                        Count = counts.TryGetValue(c.Id, out var count) ? count : 0
                    }).ToList()
                });
            }

            return points;
        }
    }

    // ── Detail ─────────────────────────────────────────────────────────────────
    public async Task<WorkItemDetailViewModel?> GetDetailAsync(Guid id)
    {
        var tid = tenant.TenantId;
        var item = await db.WorkItems
            .Include(w => w.OperationRequest)
            .Include(w => w.OrganizationUnit)
            .Include(w => w.Sprint)
            .Include(w => w.Assignments).ThenInclude(a => a.AssignedToUser)
            .Include(w => w.Checklists).ThenInclude(c => c.CompletedByUser)
            .Include(w => w.Checklists).ThenInclude(c => c.AssignedToUser)
            .Include(w => w.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tid && !w.IsDeleted);
        if (item == null) return null;

        var createdBy = item.CreatedByUserId.HasValue
            ? await db.AppUsers.Where(u => u.Id == item.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync() ?? "—"
            : "Hệ thống";

        var activeAssignment = item.Assignments.Where(a => !a.IsDeleted).OrderByDescending(a => a.AssignedAt).FirstOrDefault();
        var dependencies = await db.WorkItemDependencies
            .AsNoTracking()
            .Include(d => d.Blocker)
            .Include(d => d.Blocked)
            .Where(d => d.TenantId == tid && !d.IsDeleted && (d.BlockedId == item.Id || d.BlockerId == item.Id))
            .ToListAsync();
        var dependencyOptions = await db.WorkItems
            .AsNoTracking()
            .Where(w => w.TenantId == tid && !w.IsDeleted && w.Id != item.Id)
            .OrderByDescending(w => w.UpdatedAt ?? w.CreatedAt)
            .Select(w => new SelectOption
            {
                Value = w.Id.ToString(),
                Text = w.Title + " (" + w.Status.ToString() + ")"
            })
            .ToListAsync();

        return new WorkItemDetailViewModel
        {
            Id = item.Id, Title = item.Title, Description = item.Description,
            Status = item.Status.ToString(),
            StatusLabel = GetStatusLabel(item.Status),
            Priority = item.Priority.ToString(), PriorityClass = GetPriorityClass(item.Priority),
            Department = item.OrganizationUnit?.Name ?? "", DepartmentId = item.OrganizationUnitId,
            RequestNo = item.OperationRequest?.RequestNo ?? "", RequestTitle = item.OperationRequest?.Title ?? "",
            OperationRequestId = item.OperationRequestId,
            SprintId = item.SprintId,
            SprintName = item.Sprint?.Name,
            AssignedTo = activeAssignment?.AssignedToUser?.FullName,
            AssignedToUserId = activeAssignment?.AssignedToUserId,
            DueDate = item.DueDate,
            IsOverdue = item.DueDate.HasValue && item.DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
                && item.Status != WorkItemStatus.Done && item.Status != WorkItemStatus.Cancelled,
            CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt, CreatedByName = createdBy,
            Checklists = item.Checklists.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder)
                .Select(c => new WorkItemChecklistItem
                {
                    Id = c.Id,
                    Title = c.Title,
                    SortOrder = c.SortOrder,
                    AssignedToUserId = c.AssignedToUserId,
                    AssignedToName = c.AssignedToUser?.FullName,
                    DueDate = c.DueDate,
                    IsCompleted = c.IsCompleted,
                    CompletedByName = c.CompletedByUser?.FullName,
                    CompletedAt = c.CompletedAt
                }).ToList(),
            Comments = item.Comments.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt)
                .Select(c => new WorkItemCommentItem { Id = c.Id, Content = c.Content, UserName = c.User?.FullName ?? "", UserId = c.UserId, CreatedAt = c.CreatedAt }).ToList(),
            AssignmentHistory = item.Assignments.Where(a => !a.IsDeleted).OrderByDescending(a => a.AssignedAt)
                .Select(a => new WorkItemAssignmentItem { Id = a.Id, UserName = a.AssignedToUser?.FullName ?? "", AssignedAt = a.AssignedAt, CompletedAt = a.CompletedAt }).ToList(),
            BlockingDependencies = dependencies.Where(d => d.BlockedId == item.Id && d.Blocker is not null)
                .Select(d => new WorkItemDependencyItem
                {
                    Id = d.Id,
                    WorkItemId = d.BlockerId,
                    Title = d.Blocker!.Title,
                    Status = d.Blocker.Status,
                    StatusLabel = GetStatusLabel(d.Blocker.Status),
                    Type = d.Type
                }).ToList(),
            BlockedItems = dependencies.Where(d => d.BlockerId == item.Id && d.Blocked is not null)
                .Select(d => new WorkItemDependencyItem
                {
                    Id = d.Id,
                    WorkItemId = d.BlockedId,
                    Title = d.Blocked!.Title,
                    Status = d.Blocked.Status,
                    StatusLabel = GetStatusLabel(d.Blocked.Status),
                    Type = d.Type
                }).ToList(),
            DependencyOptions = dependencyOptions,
            ChecklistAssignees = await GetAssigneeOptionsAsync(tid)
        };
    }

    // ── Edit form ──────────────────────────────────────────────────────────────
    public async Task<WorkItemEditViewModel?> GetEditFormAsync(Guid id)
    {
        var tid = tenant.TenantId;
        var item = await db.WorkItems.Include(w => w.Assignments)
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tid && !w.IsDeleted);
        if (item == null) return null;

        var activeAssignment = item.Assignments.Where(a => !a.IsDeleted).OrderByDescending(a => a.AssignedAt).FirstOrDefault();

        return new WorkItemEditViewModel
        {
            Id = item.Id, Title = item.Title, Description = item.Description,
            OrganizationUnitId = item.OrganizationUnitId,
            AssignedToUserId = activeAssignment?.AssignedToUserId,
            Priority = item.Priority, DueDate = item.DueDate, Status = item.Status,
            KanbanColumnId = item.KanbanColumnId,
            SprintId = item.SprintId,
            Departments = await GetDepartmentOptionsAsync(tid),
            Assignees = await GetAssigneeOptionsAsync(tid),
            ColumnOptions = await db.KanbanColumns
                .Where(c => c.TenantId == tid && !c.IsDeleted)
                .OrderBy(c => c.SortOrder)
                .Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Title })
                .ToListAsync(),
            SprintOptions = await db.Sprints
                .Where(s => s.TenantId == tid && !s.IsDeleted && s.Status != SprintStatus.Closed)
                .OrderByDescending(s => s.Status == SprintStatus.Active)
                .ThenBy(s => s.StartDate)
                .Select(s => new SelectOption { Value = s.Id.ToString(), Text = s.Name + " (" + s.Status.ToString() + ")" })
                .ToListAsync(),
            StatusOptions = new[] { item.Status }
                .Concat(WorkItemStateMachine.NextStates(item.Status))
                .Distinct()
                .Select(s => new SelectOption { Value = s.ToString(), Text = GetStatusLabel(s) })
                .ToList()
        };
    }

    // ── Update ─────────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> UpdateAsync(WorkItemEditViewModel input)
    {
        var tid = tenant.TenantId;
        var item = await db.WorkItems.Include(w => w.Assignments)
            .FirstOrDefaultAsync(w => w.Id == input.Id && w.TenantId == tid && !w.IsDeleted);
        if (item == null) return (false, "Không tìm thấy công việc.");

        var oldStatus = item.Status;
        item.Title = input.Title.Trim();
        item.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        item.Priority = input.Priority;
        item.DueDate = input.DueDate;
        
        var nextStatus = input.Status;
        var nextColumnId = item.KanbanColumnId;
        var oldColumnId = item.KanbanColumnId;
        var oldSprintId = item.SprintId;
        WipLimitDecision? wipDecision = null;

        var sprint = await GetAssignableSprintAsync(input.SprintId);
        if (input.SprintId.HasValue && sprint is null)
            return (false, "Sprint không hợp lệ hoặc đã đóng.");

        if (input.KanbanColumnId.HasValue)
        {
            var col = await db.KanbanColumns
                .FirstOrDefaultAsync(c => c.Id == input.KanbanColumnId.Value && c.TenantId == tid && !c.IsDeleted);
            if (col == null) return (false, "Không tìm thấy cột Kanban.");

            if (item.KanbanColumnId != col.Id)
            {
                wipDecision = await EvaluateWipLimitAsync(col, item.Id);
                if (!wipDecision.Allowed)
                    return (false, wipDecision.Message ?? "Cột Kanban đã vượt giới hạn WIP.");
            }

            nextColumnId = col.Id;
            nextStatus = ResolveStatusForColumn(col);
        }

        if (nextStatus != oldStatus && !WorkItemStateMachine.CanTransition(oldStatus, nextStatus))
            return (false, $"Không thể chuyển công việc từ {oldStatus} sang {nextStatus}.");
        var dependencyWarning = await GetDependencyWarningAsync(item.Id, nextStatus);

        item.KanbanColumnId = nextColumnId;
        item.Status = nextStatus;
        item.SprintId = sprint?.Id;

        if (input.OrganizationUnitId.HasValue) item.OrganizationUnitId = input.OrganizationUnitId.Value;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = tenant.UserId;
        if (oldColumnId != nextColumnId)
        {
            db.WorkItemActivities.Add(new WorkItemActivity
            {
                TenantId = tid,
                WorkItemId = item.Id,
                FromColumnId = oldColumnId,
                ToColumnId = nextColumnId,
                MovedAt = item.UpdatedAt.Value,
                MovedByUserId = tenant.UserId,
                CreatedByUserId = tenant.UserId,
                CreatedAt = item.UpdatedAt.Value
            });
        }

        var currentAssignment = item.Assignments.Where(a => !a.IsDeleted).OrderByDescending(a => a.AssignedAt).FirstOrDefault();
        if (input.AssignedToUserId != currentAssignment?.AssignedToUserId)
        {
            if (input.AssignedToUserId.HasValue)
            {
                db.WorkItemAssignments.Add(new WorkItemAssignment
                {
                    TenantId = tid, WorkItemId = item.Id,
                    AssignedToUserId = input.AssignedToUserId.Value,
                    AssignedAt = DateTimeOffset.UtcNow,
                    CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        if (oldStatus != item.Status)
        {
            foreach (var a in item.Assignments.Where(a => !a.IsDeleted))
            {
                a.CompletedAt = item.Status == WorkItemStatus.Done ? DateTimeOffset.UtcNow : null;
                a.UpdatedAt = DateTimeOffset.UtcNow;
            }
            await SyncOperationStatusAsync(item);
        }

        await audit.LogAsync("WorkItem", item.Id, "Update",
            oldValueObj: new { Status = oldStatus, SprintId = oldSprintId },
            newValueObj: new { item.Title, item.Status, item.Priority, item.DueDate, item.KanbanColumnId, item.SprintId, WipWarning = wipDecision?.Message, DependencyWarning = dependencyWarning });
        if (wipDecision?.IsExceeded == true && nextColumnId.HasValue)
        {
            await audit.LogAsync("KanbanColumn", nextColumnId.Value, "WipLimitExceeded",
                newValueObj: new { wipDecision.ProjectedCount, wipDecision.Limit, wipDecision.Enforced, WorkItemId = item.Id });
        }

        var saveResult = await db.SaveChangesWithConcurrencyMessageAsync("Đã cập nhật công việc.");
        if (!saveResult.Success)
            return saveResult;

        var finalMessage = wipDecision?.IsExceeded == true
            ? wipDecision.Message ?? "Đã cập nhật công việc nhưng cột đã vượt giới hạn WIP."
            : saveResult.Message;
        if (!string.IsNullOrWhiteSpace(dependencyWarning))
            finalMessage = $"{finalMessage} {dependencyWarning}";
        return (true, finalMessage);
    }

    public async Task<(bool Success, string Message)> AddDependencyAsync(Guid blockedId, Guid blockerId, WorkItemDependencyType type)
    {
        var tid = tenant.TenantId;
        if (blockedId == Guid.Empty || blockerId == Guid.Empty)
            return (false, "Thông tin dependency không hợp lệ.");

        if (blockedId == blockerId)
            return (false, "Một thẻ không thể phụ thuộc chính nó.");

        var items = await db.WorkItems
            .Where(w => w.TenantId == tid && !w.IsDeleted && (w.Id == blockedId || w.Id == blockerId))
            .Select(w => new { w.Id, w.Title })
            .ToListAsync();
        if (items.Count != 2)
            return (false, "Không tìm thấy thẻ công việc để tạo dependency.");

        var exists = await db.WorkItemDependencies.AnyAsync(d =>
            d.TenantId == tid
            && !d.IsDeleted
            && d.BlockedId == blockedId
            && d.BlockerId == blockerId
            && d.Type == type);
        if (exists)
            return (false, "Dependency này đã tồn tại.");

        if (type == WorkItemDependencyType.BlockedBy && await CreatesBlockedByCycleAsync(blockerId, blockedId))
            return (false, "Dependency này tạo vòng lặp. Hãy kiểm tra lại quan hệ blocker/blocked.");

        var dependency = new WorkItemDependency
        {
            TenantId = tid,
            BlockerId = blockerId,
            BlockedId = blockedId,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = tenant.UserId
        };

        db.WorkItemDependencies.Add(dependency);
        await audit.LogAsync("WorkItemDependency", dependency.Id, "Create",
            newValueObj: new { dependency.BlockerId, dependency.BlockedId, dependency.Type });

        return await db.SaveChangesWithConcurrencyMessageAsync("Đã thêm dependency.");
    }

    public async Task<(bool Success, string Message)> DeleteDependencyAsync(Guid dependencyId, Guid workItemId)
    {
        var dependency = await db.WorkItemDependencies
            .FirstOrDefaultAsync(d => d.Id == dependencyId
                && d.TenantId == tenant.TenantId
                && !d.IsDeleted
                && (d.BlockedId == workItemId || d.BlockerId == workItemId));
        if (dependency is null)
            return (false, "Không tìm thấy dependency.");

        dependency.IsDeleted = true;
        dependency.UpdatedAt = DateTimeOffset.UtcNow;
        dependency.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync("WorkItemDependency", dependency.Id, "Delete",
            oldValueObj: new { dependency.BlockerId, dependency.BlockedId, dependency.Type });

        return await db.SaveChangesWithConcurrencyMessageAsync("Đã xóa dependency.");
    }

    private async Task<bool> CreatesBlockedByCycleAsync(Guid blockerId, Guid blockedId)
    {
        var edges = await db.WorkItemDependencies
            .AsNoTracking()
            .Where(d => d.TenantId == tenant.TenantId
                && !d.IsDeleted
                && d.Type == WorkItemDependencyType.BlockedBy)
            .Select(d => new { d.BlockerId, d.BlockedId })
            .ToListAsync();

        var graph = edges
            .GroupBy(e => e.BlockerId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.BlockedId).ToList());
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(blockedId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            if (!graph.TryGetValue(current, out var nextIds))
                continue;

            foreach (var nextId in nextIds)
            {
                if (nextId == blockerId)
                    return true;
                queue.Enqueue(nextId);
            }
        }

        return false;
    }

    // ── Delete ─────────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> DeleteAsync(Guid id)
    {
        var item = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenant.TenantId && !w.IsDeleted);
        if (item == null) return (false, "Không tìm thấy.");
        item.IsDeleted = true; item.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync("WorkItem", id, "Delete", oldValueObj: new { item.Title, item.Status });
        return await db.SaveChangesWithConcurrencyMessageAsync("Đã xóa công việc.");
    }

    // ── Comments ───────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> AddCommentAsync(Guid workItemId, string content)
    {
        var item = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId && w.TenantId == tenant.TenantId && !w.IsDeleted);
        if (item == null) return (false, "Không tìm thấy.");
        db.WorkItemComments.Add(new WorkItemComment
        {
            TenantId = tenant.TenantId, WorkItemId = workItemId,
            UserId = tenant.UserId, Content = content.Trim(),
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (true, "Đã thêm bình luận.");
    }

    // ── Checklist ──────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message, Guid? AssignedToUserId, string? WorkItemTitle, string? ChecklistTitle)> AddChecklistAsync(
        Guid workItemId,
        string title,
        Guid? assignedToUserId = null,
        DateOnly? dueDate = null)
    {
        var item = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId && w.TenantId == tenant.TenantId && !w.IsDeleted);
        if (item == null) return (false, "Không tìm thấy.", null, null, null);
        if (string.IsNullOrWhiteSpace(title)) return (false, "Tiêu đề checklist không được trống.", null, null, null);

        if (dueDate.HasValue && dueDate.Value < DateOnly.FromDateTime(DateTime.Today))
            return (false, "Hạn checklist không được nhỏ hơn ngày hôm nay.", null, null, null);

        var assignee = await GetChecklistAssigneeAsync(assignedToUserId);
        if (assignedToUserId.HasValue && assignee is null)
            return (false, "Người phụ trách checklist không hợp lệ.", null, null, null);

        var maxSort = await db.WorkItemChecklists.Where(c => c.WorkItemId == workItemId && !c.IsDeleted).MaxAsync(c => (int?)c.SortOrder) ?? 0;
        var checklist = new WorkItemChecklist
        {
            TenantId = tenant.TenantId, WorkItemId = workItemId,
            Title = title.Trim(), SortOrder = maxSort + 1,
            AssignedToUserId = assignee?.Id,
            DueDate = dueDate,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.WorkItemChecklists.Add(checklist);
        await audit.LogAsync("WorkItemChecklist", checklist.Id, "Create",
            newValueObj: new { checklist.WorkItemId, checklist.Title, checklist.SortOrder, checklist.AssignedToUserId, checklist.DueDate });
        var result = await db.SaveChangesWithConcurrencyMessageAsync("Đã thêm mục checklist.");
        return (result.Success, result.Message, result.Success ? assignee?.Id : null, item.Title, checklist.Title);
    }

    public async Task<(bool Success, string Message, Guid? AssignedToUserId, string? WorkItemTitle, string? ChecklistTitle)> UpdateChecklistAsync(
        Guid checklistId,
        Guid workItemId,
        string title,
        Guid? assignedToUserId,
        DateOnly? dueDate,
        int sortOrder)
    {
        var checklist = await db.WorkItemChecklists
            .Include(c => c.WorkItem)
            .FirstOrDefaultAsync(c => c.Id == checklistId
                && c.WorkItemId == workItemId
                && c.TenantId == tenant.TenantId
                && !c.IsDeleted);
        if (checklist is null || checklist.WorkItem is null)
            return (false, "Không tìm thấy mục checklist.", null, null, null);
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Tiêu đề checklist không được trống.", null, null, null);
        if (sortOrder <= 0)
            return (false, "Thứ tự checklist phải lớn hơn 0.", null, null, null);
        if (dueDate.HasValue
            && dueDate.Value < DateOnly.FromDateTime(DateTime.Today)
            && dueDate != checklist.DueDate
            && !checklist.IsCompleted)
            return (false, "Hạn checklist không được nhỏ hơn ngày hôm nay.", null, null, null);

        var assignee = await GetChecklistAssigneeAsync(assignedToUserId);
        if (assignedToUserId.HasValue && assignee is null)
            return (false, "Người phụ trách checklist không hợp lệ.", null, null, null);

        var oldAssignedToUserId = checklist.AssignedToUserId;
        var oldValue = new { checklist.Title, checklist.SortOrder, checklist.AssignedToUserId, checklist.DueDate };
        checklist.Title = title.Trim();
        checklist.SortOrder = sortOrder;
        checklist.AssignedToUserId = assignee?.Id;
        checklist.DueDate = dueDate;
        checklist.UpdatedAt = DateTimeOffset.UtcNow;
        checklist.UpdatedByUserId = tenant.UserId;

        await audit.LogAsync("WorkItemChecklist", checklist.Id, "Update",
            oldValueObj: oldValue,
            newValueObj: new { checklist.Title, checklist.SortOrder, checklist.AssignedToUserId, checklist.DueDate });
        var result = await db.SaveChangesWithConcurrencyMessageAsync("Đã cập nhật checklist.");
        var shouldNotify = result.Success && assignee?.Id != null && assignee.Id != oldAssignedToUserId;
        return (result.Success, result.Message, shouldNotify ? assignee?.Id : null, checklist.WorkItem.Title, checklist.Title);
    }

    public async Task<(bool Success, string Message)> ToggleChecklistAsync(Guid checklistId)
    {
        var cl = await db.WorkItemChecklists.FirstOrDefaultAsync(c => c.Id == checklistId && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (cl == null) return (false, "Không tìm thấy.");
        var oldValue = new { cl.IsCompleted, cl.CompletedByUserId, cl.CompletedAt };
        cl.IsCompleted = !cl.IsCompleted;
        cl.CompletedByUserId = cl.IsCompleted ? tenant.UserId : null;
        cl.CompletedAt = cl.IsCompleted ? DateTimeOffset.UtcNow : null;
        cl.UpdatedAt = DateTimeOffset.UtcNow;
        cl.UpdatedByUserId = tenant.UserId;
        await audit.LogAsync("WorkItemChecklist", cl.Id, cl.IsCompleted ? "Complete" : "Reopen",
            oldValueObj: oldValue,
            newValueObj: new { cl.IsCompleted, cl.CompletedByUserId, cl.CompletedAt });
        return await db.SaveChangesWithConcurrencyMessageAsync(cl.IsCompleted ? "Đã hoàn thành." : "Đã bỏ hoàn thành.");
    }

    public async Task<(bool Success, string Message)> DeleteChecklistAsync(Guid checklistId)
    {
        var cl = await db.WorkItemChecklists.FirstOrDefaultAsync(c => c.Id == checklistId && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (cl == null) return (false, "Không tìm thấy.");
        cl.IsDeleted = true;
        cl.UpdatedAt = DateTimeOffset.UtcNow;
        cl.UpdatedByUserId = tenant.UserId;
        await audit.LogAsync("WorkItemChecklist", checklistId, "Delete",
            oldValueObj: new { cl.WorkItemId, cl.Title, cl.SortOrder, cl.AssignedToUserId, cl.DueDate, cl.IsCompleted });
        return await db.SaveChangesWithConcurrencyMessageAsync("Đã xóa mục checklist.");
    }

    private Task<AppUser?> GetChecklistAssigneeAsync(Guid? assignedToUserId)
    {
        if (!assignedToUserId.HasValue)
            return Task.FromResult<AppUser?>(null);

        return db.AppUsers.FirstOrDefaultAsync(u =>
            u.Id == assignedToUserId.Value
            && u.TenantId == tenant.TenantId
            && u.Status == UserStatus.Active
            && !u.IsDeleted);
    }

    private static string GetStatusLabel(WorkItemStatus s) => s switch
    {
        WorkItemStatus.Todo => "Cần làm",
        WorkItemStatus.InProgress => "Đang xử lý",
        WorkItemStatus.Blocked => "Đang vướng",
        WorkItemStatus.Done => "Hoàn thành",
        WorkItemStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    private static string GetSprintStatusLabel(SprintStatus status) => status switch
    {
        SprintStatus.Planned => "Kế hoạch",
        SprintStatus.Active => "Đang chạy",
        SprintStatus.Closed => "Đã đóng",
        _ => status.ToString()
    };

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}

// ─── Approval ────────────────────────────────────────────────────────────────
public class ApprovalService(
    ApplicationDbContext db,
    ITenantContext tenant,
    IAuditService audit,
    IOperationSlaService operationSla,
    IOperationSlaWatcherQueue slaWatcherQueue,
    CriticalPathCalculator criticalPath)
{
    private const string SalesOrderTargetType = "SalesOrder";
    private const string OperationRequestTargetType = "OperationRequest";
    private const string OperationPlanTargetType = "OperationPlan";
    private const string DepartmentReviewStepCode = "DEPARTMENT_REVIEW";
    private const string ExecutiveReviewStepCode = "EXECUTIVE_REVIEW";
    private const string PlanReviewStepCode = "PLAN_REVIEW";

    private static string StepNameFor(string targetType, string stepCode) =>
        targetType switch
        {
            SalesOrderTargetType => stepCode == DepartmentReviewStepCode ? "Trưởng bộ phận duyệt đơn hàng" : "Ban giám đốc duyệt đơn hàng",
            OperationPlanTargetType => "Duyệt kế hoạch vận hành",
            _ => stepCode == DepartmentReviewStepCode ? "Trưởng bộ phận duyệt" : "Ban lãnh đạo duyệt"
        };

    private static string WorkflowNameFor(string targetType) =>
        targetType switch
        {
            SalesOrderTargetType => "Quy trình phê duyệt đơn hàng",
            OperationPlanTargetType => "Quy trình duyệt kế hoạch vận hành",
            _ => "Quy trình phê duyệt yêu cầu vận hành"
        };

    private async Task<decimal> CalculateOperationEstimatedCostAsync(Guid requestId)
    {
        var lines = await db.OperationRequestLines
            .Where(l => l.TenantId == tenant.TenantId && l.OperationRequestId == requestId && !l.IsDeleted)
            .Select(l => new { l.Quantity, l.UnitPrice, l.LineAmount })
            .ToListAsync();

        return lines.Sum(l => l.LineAmount ?? l.Quantity * (l.UnitPrice ?? 0m));
    }

    private async Task<int> EnsureOperationPlanBaselinesAsync(OperationPlan plan, DateTimeOffset snapshotAt)
    {
        var taskIds = plan.Tasks.Where(t => !t.IsDeleted).Select(t => t.Id).ToList();
        if (!taskIds.Any()) return 0;

        var existingTaskIds = await db.PlanTaskBaselines
            .Where(b => b.TenantId == tenant.TenantId && b.PlanId == plan.Id && taskIds.Contains(b.PlanTaskId) && !b.IsDeleted)
            .Select(b => b.PlanTaskId)
            .ToListAsync();

        var missingTasks = plan.Tasks
            .Where(t => !t.IsDeleted && !existingTaskIds.Contains(t.Id))
            .ToList();

        foreach (var task in missingTasks)
        {
            db.PlanTaskBaselines.Add(new PlanTaskBaseline
            {
                TenantId = tenant.TenantId,
                PlanId = plan.Id,
                PlanTaskId = task.Id,
                TaskName = task.Name,
                BaselineStart = task.StartTime,
                BaselineEnd = task.EndTime,
                BaselineAssignedUserId = task.AssignedUserId,
                BaselineEquipmentId = task.EquipmentId,
                SnapshottedAt = snapshotAt,
                SnapshottedByUserId = tenant.UserId,
                CreatedAt = snapshotAt,
                CreatedByUserId = tenant.UserId
            });
        }

        if (missingTasks.Any())
        {
            await audit.LogAsync("OperationPlan", plan.Id, "CreateBaseline",
                newValueObj: new { BaselineTaskCount = missingTasks.Count, SnapshottedAt = snapshotAt });
        }

        return missingTasks.Count;
    }

    private async Task<CriticalPathResult> RecalculateOperationPlanCriticalPathAsync(OperationPlan plan)
    {
        var dependencies = await db.PlanTaskDependencies
            .Where(d => d.TenantId == tenant.TenantId && d.PlanId == plan.Id && !d.IsDeleted)
            .ToListAsync();

        var result = criticalPath.Calculate(plan.Tasks.ToList(), dependencies, DateTime.UtcNow);
        if (result.HasCycle) return result;

        if (plan.ProjectedEndDate != result.ProjectedEndDate)
        {
            plan.ProjectedEndDate = result.ProjectedEndDate;
        }

        foreach (var task in plan.Tasks)
        {
            if (result.Tasks.TryGetValue(task.Id, out var schedule))
            {
                if (task.EarlyStart != schedule.EarlyStart) task.EarlyStart = schedule.EarlyStart;
                if (task.EarlyFinish != schedule.EarlyFinish) task.EarlyFinish = schedule.EarlyFinish;
                if (task.LateStart != schedule.LateStart) task.LateStart = schedule.LateStart;
                if (task.LateFinish != schedule.LateFinish) task.LateFinish = schedule.LateFinish;
                if (task.SlackMinutes != schedule.SlackMinutes) task.SlackMinutes = schedule.SlackMinutes;
                if (task.IsCriticalPath != schedule.IsCritical) task.IsCriticalPath = schedule.IsCritical;
            }
        }

        return result;
    }

    public async Task<ApprovalTaskListViewModel> GetMyTasksAsync(string? search = null, string? statusFilter = null)
    {
        var tid = tenant.TenantId;
        var userRoles = tenant.Roles.ToList();
        var query = db.ApprovalTasks.Where(t => t.TenantId == tid && !t.IsDeleted &&
                (t.AssignedToUserId == tenant.UserId || (t.AssignedRole != null && userRoles.Contains(t.AssignedRole))));

        var allTasks = await query.Include(t => t.AssignedToUser)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();

        // Enforce department manager isolation for department approvals
        if (userRoles.Contains("DEPARTMENT_MANAGER") && !userRoles.Contains("EXECUTIVE") && !userRoles.Contains("TENANT_ADMIN") && !userRoles.Contains("SYSTEM_ADMIN"))
        {
            var deptIds = await db.EmployeeProfiles
                .Where(ep => ep.UserId == tenant.UserId && !ep.IsDeleted)
                .SelectMany(ep => ep.DepartmentAssignments.Where(da => !da.IsDeleted).Select(da => da.OrganizationUnitId))
                .ToListAsync();

            var reqIdsForFilter = allTasks.Where(t => t.TargetType == OperationRequestTargetType).Select(t => t.TargetId).Distinct().ToList();
            var reqDepts = await db.OperationRequests
                .Where(r => reqIdsForFilter.Contains(r.Id))
                .Select(r => new { r.Id, r.OrganizationUnitId })
                .ToDictionaryAsync(x => x.Id, x => x.OrganizationUnitId);

            allTasks = allTasks.Where(t =>
            {
                if (t.AssignedRole != "DEPARTMENT_MANAGER") return true;
                if (t.TargetType == OperationRequestTargetType)
                {
                    if (reqDepts.TryGetValue(t.TargetId, out var reqDeptId))
                    {
                        return deptIds.Contains(reqDeptId);
                    }
                    return false;
                }
                return true;
            }).ToList();
        }

        // Stats (after department filtering)
        var pendingCount = allTasks.Count(t => t.Status == ApprovalStatus.Pending);
        var approvedCount = allTasks.Count(t => t.Status == ApprovalStatus.Approved);
        var rejectedCount = allTasks.Count(t => t.Status == ApprovalStatus.Rejected);
        var totalCount = allTasks.Count;

        var reqIds = allTasks.Where(t => t.TargetType == OperationRequestTargetType).Select(t => t.TargetId).Distinct().ToList();
        var reqs = await db.OperationRequests
            .Include(r => r.OrganizationUnit)
            .Where(r => reqIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        var soIds = allTasks.Where(t => t.TargetType == SalesOrderTargetType).Select(t => t.TargetId).Distinct().ToList();
        var salesOrders = await db.SalesOrders
            .Include(o => o.Customer)
            .Where(o => soIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id);

        var planIds = allTasks.Where(t => t.TargetType == OperationPlanTargetType).Select(t => t.TargetId).Distinct().ToList();
        var operationPlans = await db.OperationPlans
            .Where(p => planIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Creator lookup
        var creatorIds = reqs.Values.Where(r => r.CreatedByUserId.HasValue).Select(r => r.CreatedByUserId!.Value)
            .Concat(salesOrders.Values.Where(o => o.CreatedByUserId.HasValue).Select(o => o.CreatedByUserId!.Value))
            .Concat(operationPlans.Values.Where(p => p.CreatedByUserId.HasValue).Select(p => p.CreatedByUserId!.Value))
            .Distinct().ToList();
        var creators = await db.AppUsers.Where(u => creatorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        ApprovalTaskItem Map(ApprovalTask t) {
            if (t.TargetType == SalesOrderTargetType && salesOrders.TryGetValue(t.TargetId, out var so))
            {
                var createdByName = so.CreatedByUserId.HasValue && creators.TryGetValue(so.CreatedByUserId.Value, out var n) ? n : null;
                return new ApprovalTaskItem
                {
                    Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode,
                    StepName = StepNameFor(t.TargetType, t.StepCode),
                    Status = t.Status.ToString(), AssignedRole = t.AssignedRole,
                    AssignedToName = t.AssignedToUser?.FullName,
                    DecisionNote = t.DecisionNote, DecidedAt = t.DecidedAt, CreatedAt = t.CreatedAt,
                    RequestTitle = $"Đơn hàng {so.OrderNo}", RequestNo = so.OrderNo,
                    RequestPriority = "Normal",
                    RequestCreatedAt = so.CreatedAt,
                    RequestDescription = $"Khách hàng: {so.Customer?.Name ?? "N/A"}. Tổng tiền: {so.TotalAmount:N0} VND. {so.Notes}",
                    RequestDepartment = "Kinh doanh / Sản xuất",
                    RequestCreatedBy = createdByName
                };
            }

            if (t.TargetType == OperationPlanTargetType && operationPlans.TryGetValue(t.TargetId, out var plan))
            {
                var createdByName = plan.CreatedByUserId.HasValue && creators.TryGetValue(plan.CreatedByUserId.Value, out var planCreatorName) ? planCreatorName : null;
                return new ApprovalTaskItem
                {
                    Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode,
                    StepName = StepNameFor(t.TargetType, t.StepCode),
                    Status = t.Status.ToString(), AssignedRole = t.AssignedRole,
                    AssignedToName = t.AssignedToUser?.FullName,
                    DecisionNote = t.DecisionNote, DecidedAt = t.DecidedAt, CreatedAt = t.CreatedAt,
                    RequestTitle = plan.Title, RequestNo = plan.Code,
                    RequestPriority = "Normal",
                    RequestCreatedAt = plan.CreatedAt,
                    RequestDescription = $"Loại: {plan.PlanType}. Thời gian: {plan.StartDate:dd/MM/yyyy} - {plan.EndDate:dd/MM/yyyy}. {plan.Notes}",
                    RequestDepartment = "Vận hành / Kế hoạch",
                    RequestCreatedBy = createdByName
                };
            }

            reqs.TryGetValue(t.TargetId, out var req);
            var reqCreatedByName = req?.CreatedByUserId.HasValue == true && creators.TryGetValue(req.CreatedByUserId!.Value, out var rn) ? rn : null;
            return new ApprovalTaskItem
            {
                Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode,
                StepName = StepNameFor(t.TargetType, t.StepCode),
                Status = t.Status.ToString(), AssignedRole = t.AssignedRole,
                AssignedToName = t.AssignedToUser?.FullName,
                DecisionNote = t.DecisionNote, DecidedAt = t.DecidedAt, CreatedAt = t.CreatedAt,
                RequestTitle = req?.Title ?? "", RequestNo = req?.RequestNo ?? "",
                RequestPriority = req?.Priority.ToString() ?? "",
                RequestCreatedAt = req?.CreatedAt ?? DateTimeOffset.MinValue,
                RequestDescription = req?.Description,
                RequestDepartment = req?.OrganizationUnit?.Name,
                RequestCreatedBy = reqCreatedByName
            };
        }

        var pending = allTasks.Where(t => t.Status == ApprovalStatus.Pending).Select(Map).ToList();
        var completed = allTasks.Where(t => t.Status != ApprovalStatus.Pending).Select(Map).ToList();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            pending = pending.Where(t => t.RequestNo.Contains(search, StringComparison.OrdinalIgnoreCase) || t.RequestTitle.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            completed = completed.Where(t => t.RequestNo.Contains(search, StringComparison.OrdinalIgnoreCase) || t.RequestTitle.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            completed = completed.Where(t => t.Status == statusFilter).ToList();
        }

        return new ApprovalTaskListViewModel
        {
            PendingTasks = pending, CompletedTasks = completed,
            PendingCount = pendingCount, ApprovedCount = approvedCount, RejectedCount = rejectedCount, TotalCount = totalCount,
            SearchTerm = search, StatusFilter = statusFilter
        };
    }

    public async Task<ApprovalTaskDetailViewModel?> GetDetailAsync(Guid id)
    {
        var tid = tenant.TenantId;
        var t = await db.ApprovalTasks
            .Include(a => a.AssignedToUser)
            .Include(a => a.WorkflowInstance).ThenInclude(w => w!.WorkflowDefinition)
            .Include(a => a.WorkflowInstance).ThenInclude(w => w!.ApprovalTasks).ThenInclude(at => at.AssignedToUser)
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tid && !a.IsDeleted);
        if (t == null) return null;

        string title = "";
        string no = "";
        string? desc = null;
        string priority = "Normal";
        string? dept = null;
        string? createdByName = null;
        DateTimeOffset reqCreatedAt = DateTimeOffset.MinValue;
        string status = "";

        if (t.TargetType == SalesOrderTargetType)
        {
            var so = await db.SalesOrders.Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == t.TargetId);
            if (so != null)
            {
                title = $"Đơn hàng {so.OrderNo}";
                no = so.OrderNo;
                desc = $"Khách hàng: {so.Customer?.Name ?? "N/A"}. Tổng tiền: {so.TotalAmount:N0} VND. {so.Notes}";
                dept = "Kinh doanh / Sản xuất";
                reqCreatedAt = so.CreatedAt;
                status = so.Status.ToString();
                createdByName = so.CreatedByUserId.HasValue
                    ? await db.AppUsers.Where(u => u.Id == so.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync() : null;
            }
        }
        else if (t.TargetType == OperationPlanTargetType)
        {
            var plan = await db.OperationPlans.FirstOrDefaultAsync(p => p.Id == t.TargetId);
            if (plan != null)
            {
                title = plan.Title;
                no = plan.Code;
                desc = $"Loại: {plan.PlanType}. Thời gian: {plan.StartDate:dd/MM/yyyy} - {plan.EndDate:dd/MM/yyyy}. {plan.Notes}";
                dept = "Vận hành / Kế hoạch";
                reqCreatedAt = plan.CreatedAt;
                status = plan.Status.ToString();
                createdByName = plan.CreatedByUserId.HasValue
                    ? await db.AppUsers.Where(u => u.Id == plan.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync() : null;
            }
        }
        else
        {
            var req = await db.OperationRequests.Include(r => r.OrganizationUnit)
                .FirstOrDefaultAsync(r => r.Id == t.TargetId);
            if (req != null)
            {
                title = req.Title;
                no = req.RequestNo;
                desc = req.Description;
                priority = req.Priority.ToString();
                dept = req.OrganizationUnit?.Name;
                reqCreatedAt = req.CreatedAt;
                status = req.Status.ToString();
                createdByName = req.CreatedByUserId.HasValue
                    ? await db.AppUsers.Where(u => u.Id == req.CreatedByUserId!.Value).Select(u => u.FullName).FirstOrDefaultAsync() : null;
            }
        }

        // Get all steps in this workflow
        var allSteps = t.WorkflowInstance?.ApprovalTasks?
            .Where(a => !a.IsDeleted).OrderBy(a => a.CreatedAt)
            .Select(a => new ApprovalStepItem
            {
                Id = a.Id, StepCode = a.StepCode,
                StepName = StepNameFor(a.TargetType, a.StepCode),
                Status = a.Status.ToString(),
                AssignedToName = a.AssignedToUser?.FullName, AssignedRole = a.AssignedRole,
                DecisionNote = a.DecisionNote, DecidedAt = a.DecidedAt, CreatedAt = a.CreatedAt,
                IsCurrent = a.Id == t.Id
            }).ToList() ?? new();

        // Available assignees for reassign
        var assignees = t.Status == ApprovalStatus.Pending
            ? await db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted && u.Status == UserStatus.Active)
                .OrderBy(u => u.FullName)
                .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName }).ToListAsync()
            : new();

        return new ApprovalTaskDetailViewModel
        {
            Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode,
            StepName = StepNameFor(t.TargetType, t.StepCode),
            Status = t.Status.ToString(),
            StatusLabel = t.Status switch { ApprovalStatus.Pending => "Chờ duyệt", ApprovalStatus.Approved => "Đã duyệt", ApprovalStatus.Rejected => "Từ chối", ApprovalStatus.Skipped => "Bỏ qua", ApprovalStatus.Cancelled => "Đã hủy", _ => t.Status.ToString() },
            AssignedRole = t.AssignedRole, AssignedToName = t.AssignedToUser?.FullName, AssignedToUserId = t.AssignedToUserId,
            DecisionNote = t.DecisionNote, DecidedAt = t.DecidedAt, CreatedAt = t.CreatedAt,
            RequestTitle = title, RequestNo = no,
            RequestDescription = desc, RequestPriority = priority,
            RequestDepartment = dept,
            RequestCreatedBy = createdByName,
            RequestCreatedAt = reqCreatedAt,
            RequestStatus = status,
            WorkflowName = t.WorkflowInstance?.WorkflowDefinition?.Name ?? WorkflowNameFor(t.TargetType),
            WorkflowStatus = t.WorkflowInstance?.Status.ToString(),
            AllSteps = allSteps, AvailableAssignees = assignees,
            NextStatuses = ApprovalTaskStateMachine.NextStates(t.Status).Select(s => s.ToString()).ToList()
        };
    }

    public async Task<bool> ApproveAsync(Guid taskId, string? note)
    {
        var t = await db.ApprovalTasks.Include(a => a.WorkflowInstance).FirstOrDefaultAsync(a => a.Id == taskId);
        if (t is null || t.TenantId != tenant.TenantId || !ApprovalTaskStateMachine.CanTransition(t.Status, ApprovalStatus.Approved)) return false;

        t.Status = ApprovalStatus.Approved;
        t.DecisionNote = note;
        t.DecidedAt = DateTimeOffset.UtcNow;
        t.UpdatedAt = DateTimeOffset.UtcNow;

        var shouldQueueSlaCheck = false;
        if (t.TargetType == SalesOrderTargetType)
        {
            var so = await db.SalesOrders.FindAsync(t.TargetId);
            if (so != null)
            {
                if (t.StepCode == DepartmentReviewStepCode)
                {
                    // Department Manager approved, now escalate to Executive
                    var nextTask = new ApprovalTask
                    {
                        TenantId = tenant.TenantId,
                        WorkflowInstanceId = t.WorkflowInstanceId,
                        TargetType = SalesOrderTargetType,
                        TargetId = so.Id,
                        StepCode = ExecutiveReviewStepCode,
                        AssignedRole = "EXECUTIVE",
                        Status = ApprovalStatus.Pending
                    };
                    db.ApprovalTasks.Add(nextTask);
                    so.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else if (t.StepCode == ExecutiveReviewStepCode)
                {
                    // Executive approved, Order is fully approved
                    so.Status = SalesOrderStatus.Approved;
                    so.UpdatedAt = DateTimeOffset.UtcNow;

                    if (t.WorkflowInstance != null)
                    {
                        t.WorkflowInstance.Status = WorkflowInstanceStatus.Completed;
                        t.WorkflowInstance.CompletedAt = DateTimeOffset.UtcNow;
                    }
                }
            }
        }
        else if (t.TargetType == OperationPlanTargetType)
        {
            var plan = await db.OperationPlans
                .Include(p => p.Tasks.Where(task => !task.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == t.TargetId && p.TenantId == tenant.TenantId && !p.IsDeleted);
            if (plan != null)
            {
                var oldPlanStatus = plan.Status;
                if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.Approved)) return false;

                plan.Status = OperationPlanStatus.Approved;
                plan.UpdatedAt = DateTimeOffset.UtcNow;
                plan.UpdatedByUserId = tenant.UserId;
                var baselineCount = await EnsureOperationPlanBaselinesAsync(plan, DateTimeOffset.UtcNow);
                var criticalPathResult = await RecalculateOperationPlanCriticalPathAsync(plan);

                await audit.LogAsync("OperationPlan", plan.Id, "Approve",
                    oldValueObj: new { Status = oldPlanStatus },
                    newValueObj: new { plan.Status },
                    extra: new { ApprovalTaskId = taskId, BaselineTaskCount = baselineCount, criticalPathResult.ProjectedEndDate, Note = note });

                if (t.WorkflowInstance != null)
                {
                    t.WorkflowInstance.Status = WorkflowInstanceStatus.Completed;
                    t.WorkflowInstance.CompletedAt = DateTimeOffset.UtcNow;
                }
            }
        }
        else
        {
            var req = await db.OperationRequests.FindAsync(t.TargetId);
            if (req != null)
            {
                OperationStatus nextRequestStatus;
                if (t.StepCode == DepartmentReviewStepCode)
                {
                    // Multi-step: if total amount > 50,000,000, escalate to EXECUTIVE
                    if (req.TotalAmount > 50000000)
                    {
                        var nextTask = new ApprovalTask
                        {
                            TenantId = tenant.TenantId,
                            WorkflowInstanceId = t.WorkflowInstanceId,
                            TargetType = OperationRequestTargetType,
                            TargetId = req.Id,
                            StepCode = ExecutiveReviewStepCode,
                            AssignedRole = "EXECUTIVE",
                            Status = ApprovalStatus.Pending,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        db.ApprovalTasks.Add(nextTask);
                        nextRequestStatus = OperationStatus.InReview;
                    }
                    else
                    {
                        nextRequestStatus = OperationStatus.Approved;
                    }
                }
                else if (t.StepCode == ExecutiveReviewStepCode)
                {
                    nextRequestStatus = OperationStatus.Approved;
                }
                else
                {
                    nextRequestStatus = OperationStatus.Approved;
                }

                if (!OperationRequestStateMachine.CanTransition(req.Status, nextRequestStatus)) return false;
                var decidedAt = DateTimeOffset.UtcNow;
                req.Status = nextRequestStatus;
                req.UpdatedAt = decidedAt;
                if (nextRequestStatus == OperationStatus.Approved)
                {
                    req.EstimatedCost = await CalculateOperationEstimatedCostAsync(req.Id);
                    await operationSla.ApplyApprovedAsync(req, decidedAt);
                    shouldQueueSlaCheck = true;
                }
            }
        }

        await audit.LogAsync("ApprovalTask", taskId, "Approve",
            newValueObj: new { Status = ApprovalStatus.Approved, t.TargetType, t.TargetId, t.StepCode });
        var saved = await db.SaveChangesWithConcurrencyAsync();
        if (saved && shouldQueueSlaCheck) slaWatcherQueue.TryQueue("operation-request-approved");
        return saved;
    }

    public async Task<bool> RejectAsync(Guid taskId, string reason)
    {
        var t = await db.ApprovalTasks.Include(a => a.WorkflowInstance).FirstOrDefaultAsync(a => a.Id == taskId);
        if (t is null || t.TenantId != tenant.TenantId || !ApprovalTaskStateMachine.CanTransition(t.Status, ApprovalStatus.Rejected)) return false;

        t.Status = ApprovalStatus.Rejected;
        t.DecisionNote = reason;
        t.DecidedAt = DateTimeOffset.UtcNow;
        t.UpdatedAt = DateTimeOffset.UtcNow;

        if (t.TargetType == SalesOrderTargetType)
        {
            var so = await db.SalesOrders.FindAsync(t.TargetId);
            if (so != null)
            {
                so.Status = SalesOrderStatus.Cancelled;
                so.UpdatedAt = DateTimeOffset.UtcNow;
            }
            if (t.WorkflowInstance != null)
            {
                t.WorkflowInstance.Status = WorkflowInstanceStatus.Rejected;
                t.WorkflowInstance.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
        else if (t.TargetType == OperationPlanTargetType)
        {
            var plan = await db.OperationPlans.FindAsync(t.TargetId);
            if (plan != null)
            {
                var oldPlanStatus = plan.Status;
                if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.Draft)) return false;
                plan.Status = OperationPlanStatus.Draft;
                plan.UpdatedAt = DateTimeOffset.UtcNow;
                plan.UpdatedByUserId = tenant.UserId;

                await audit.LogAsync("OperationPlan", plan.Id, "Reject",
                    oldValueObj: new { Status = oldPlanStatus },
                    newValueObj: new { plan.Status },
                    extra: new { ApprovalTaskId = taskId, Reason = reason });
            }
            if (t.WorkflowInstance != null)
            {
                t.WorkflowInstance.Status = WorkflowInstanceStatus.Rejected;
                t.WorkflowInstance.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            var req = await db.OperationRequests.FindAsync(t.TargetId);
            if (req != null)
            {
                if (!OperationRequestStateMachine.CanTransition(req.Status, OperationStatus.Rejected)) return false;
                req.Status = OperationStatus.Rejected;
                req.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await audit.LogAsync("ApprovalTask", taskId, "Reject",
            newValueObj: new { Status = ApprovalStatus.Rejected, Reason = reason, t.TargetType, t.TargetId, t.StepCode });
        return await db.SaveChangesWithConcurrencyAsync();
    }

    public async Task<(bool Success, string Message)> ReassignAsync(Guid taskId, Guid newUserId)
    {
        var t = await db.ApprovalTasks.FindAsync(taskId);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != ApprovalStatus.Pending)
            return (false, "Không thể chuyển giao.");
        var newUser = await db.AppUsers.FindAsync(newUserId);
        if (newUser == null) return (false, "Không tìm thấy người dùng.");

        var oldUserId = t.AssignedToUserId;
        t.AssignedToUserId = newUserId;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        await audit.LogAsync("ApprovalTask", taskId, "Reassign",
            oldValueObj: new { AssignedToUserId = oldUserId },
            newValueObj: new { AssignedToUserId = newUserId, AssignedToName = newUser.FullName });
        return await db.SaveChangesWithConcurrencyMessageAsync($"Đã chuyển giao cho {newUser.FullName}.");
    }

    public async Task<(bool Success, string Message)> ReturnForRevisionAsync(Guid taskId, string reason)
    {
        var t = await db.ApprovalTasks.Include(a => a.WorkflowInstance).FirstOrDefaultAsync(a => a.Id == taskId);
        if (t is null || t.TenantId != tenant.TenantId || !ApprovalTaskStateMachine.CanTransition(t.Status, ApprovalStatus.Skipped))
            return (false, "Không thể trả lại.");
        t.Status = ApprovalStatus.Skipped;
        t.DecisionNote = $"Trả lại: {reason}";
        t.DecidedAt = DateTimeOffset.UtcNow;
        t.UpdatedAt = DateTimeOffset.UtcNow;

        if (t.TargetType == SalesOrderTargetType)
        {
            var so = await db.SalesOrders.FindAsync(t.TargetId);
            if (so != null)
            {
                so.Status = SalesOrderStatus.Draft;
                so.UpdatedAt = DateTimeOffset.UtcNow;
            }
            if (t.WorkflowInstance != null)
            {
                t.WorkflowInstance.Status = WorkflowInstanceStatus.Cancelled;
                t.WorkflowInstance.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
        else if (t.TargetType == OperationPlanTargetType)
        {
            var plan = await db.OperationPlans.FindAsync(t.TargetId);
            if (plan != null)
            {
                var oldPlanStatus = plan.Status;
                if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.Draft))
                    return (false, "Trạng thái kế hoạch hiện tại không cho phép trả lại chỉnh sửa.");

                plan.Status = OperationPlanStatus.Draft;
                plan.UpdatedAt = DateTimeOffset.UtcNow;
                plan.UpdatedByUserId = tenant.UserId;

                await audit.LogAsync("OperationPlan", plan.Id, "ReturnForRevision",
                    oldValueObj: new { Status = oldPlanStatus },
                    newValueObj: new { plan.Status },
                    extra: new { ApprovalTaskId = taskId, Reason = reason });
            }
            if (t.WorkflowInstance != null)
            {
                t.WorkflowInstance.Status = WorkflowInstanceStatus.Cancelled;
                t.WorkflowInstance.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            var req = await db.OperationRequests.FindAsync(t.TargetId);
            if (req != null)
            {
                if (!OperationRequestStateMachine.CanTransition(req.Status, OperationStatus.Draft))
                    return (false, "Trạng thái yêu cầu hiện tại không cho phép trả lại chỉnh sửa.");

                req.Status = OperationStatus.Draft;
                req.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await audit.LogAsync("ApprovalTask", taskId, "ReturnForRevision",
            newValueObj: new { Status = ApprovalStatus.Skipped, Reason = reason, t.TargetType, t.TargetId, t.StepCode });
        return await db.SaveChangesWithConcurrencyMessageAsync("Đã trả lại yêu cầu để chỉnh sửa.");
    }

    // ── BULK APPROVE / REJECT (F6.4) ─────────────────────────────────────────
    public async Task<Models.Common.BulkResult> BulkApproveAsync(List<Guid> taskIds, string? note)
    {
        int success = 0, fail = 0;
        var errors = new List<string>();
        foreach (var id in taskIds)
        {
            try
            {
                var ok = await ApproveAsync(id, note);
                if (ok) success++;
                else { fail++; errors.Add($"Task {id}: không thể duyệt (trạng thái không hợp lệ)."); }
            }
            catch (Exception ex)
            {
                fail++;
                errors.Add($"Task {id}: {ex.Message}");
            }
        }
        return Models.Common.BulkResult.From(success, fail, errors);
    }

    public async Task<Models.Common.BulkResult> BulkRejectAsync(List<Guid> taskIds, string reason)
    {
        int success = 0, fail = 0;
        var errors = new List<string>();
        foreach (var id in taskIds)
        {
            try
            {
                var ok = await RejectAsync(id, reason);
                if (ok) success++;
                else { fail++; errors.Add($"Task {id}: không thể từ chối."); }
            }
            catch (Exception ex)
            {
                fail++;
                errors.Add($"Task {id}: {ex.Message}");
            }
        }
        return Models.Common.BulkResult.From(success, fail, errors);
    }

    // ── APPROVAL TIMELINE (F6.6) ─────────────────────────────────────────────
    public async Task<List<ApprovalStepItem>> GetTimelineAsync(string targetType, Guid targetId)
    {
        var tid = tenant.TenantId;
        var tasks = await db.ApprovalTasks
            .Include(a => a.AssignedToUser)
            .Where(a => a.TenantId == tid && a.TargetType == targetType && a.TargetId == targetId && !a.IsDeleted)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        return tasks.Select(a => new ApprovalStepItem
        {
            Id = a.Id,
            StepCode = a.StepCode,
            StepName = StepNameFor(a.TargetType, a.StepCode),
            Status = a.Status.ToString(),
            AssignedToName = a.AssignedToUser?.FullName,
            AssignedRole = a.AssignedRole,
            DecisionNote = a.DecisionNote,
            DecidedAt = a.DecidedAt,
            CreatedAt = a.CreatedAt,
            IsCurrent = a.Status == ApprovalStatus.Pending
        }).ToList();
    }
}

// ─── AI Insight — Real Gemini Integration ────────────────────────────────────
public class AiInsightService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini, IAuditService audit)
{
    public async Task<List<AiInsightListItem>> GetListAsync() =>
        await db.AiInsights.Where(a => a.TenantId == tenant.TenantId && !a.IsDeleted).OrderByDescending(a => a.CreatedAt)
            .Select(a => new AiInsightListItem { Id = a.Id, ContextType = a.ContextType, Question = a.Question, Summary = a.Summary, Recommendation = a.Recommendation, RiskLevel = a.RiskLevel.ToString(), Status = a.Status.ToString(), ModelName = "gemini", AskedByName = a.AskedByUser != null ? a.AskedByUser.FullName : null, CreatedAt = a.CreatedAt })
            .ToListAsync();

    public async Task<AiInsightIndexViewModel> GetFilteredListAsync(string? contextType, string? riskLevel, string? search, int page = 1)
    {
        var query = db.AiInsights.Where(a => a.TenantId == tenant.TenantId && !a.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(contextType)) query = query.Where(a => a.ContextType == contextType);
        if (!string.IsNullOrWhiteSpace(riskLevel) && Enum.TryParse<RiskLevel>(riskLevel, out var rl)) query = query.Where(a => a.RiskLevel == rl);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(a => a.Question.Contains(search) || a.Summary.Contains(search));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * 20).Take(20)
            .Select(a => new AiInsightListItem { Id = a.Id, ContextType = a.ContextType, Question = a.Question, Summary = a.Summary.Length > 200 ? a.Summary.Substring(0, 200) + "..." : a.Summary, Recommendation = a.Recommendation, RiskLevel = a.RiskLevel.ToString(), Status = a.Status.ToString(), ModelName = "gemini", AskedByName = a.AskedByUser != null ? a.AskedByUser.FullName : null, CreatedAt = a.CreatedAt })
            .ToListAsync();
        return new AiInsightIndexViewModel { Items = items, TotalCount = total, Page = page, ContextTypeFilter = contextType, RiskLevelFilter = riskLevel, SearchTerm = search };
    }

    public async Task<AiInsightDetailViewModel?> GetDetailAsync(Guid id)
    {
        return await db.AiInsights.Where(a => a.Id == id && a.TenantId == tenant.TenantId && !a.IsDeleted)
            .Select(a => new AiInsightDetailViewModel { Id = a.Id, ContextType = a.ContextType, Question = a.Question, Summary = a.Summary, Recommendation = a.Recommendation, RiskLevel = a.RiskLevel.ToString(), Status = a.Status.ToString(), ModelName = "gemini", AskedByName = a.AskedByUser != null ? a.AskedByUser.FullName : null, RawResponseJson = a.RawResponseJson, CreatedAt = a.CreatedAt })
            .FirstOrDefaultAsync();
    }

    public async Task<AiInsightListItem> AnalyzeAsync(AiInsightCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var ctx = await BuildContextAsync(tid, today);
        var sysPrompt = SystemPrompt();
        var userPrompt = UserPrompt(vm, ctx);

        var result = await gemini.GenerateAsync(sysPrompt, userPrompt, 0.4, 3000);

        string summary; string? recommendation; var risk = RiskLevel.Low; var status = AiInsightStatus.Draft;

        if (result.Success)
        {
            var t = result.Text;
            if (t.Contains("---RISK:HIGH---", StringComparison.OrdinalIgnoreCase)) risk = RiskLevel.High;
            else if (t.Contains("---RISK:MEDIUM---", StringComparison.OrdinalIgnoreCase)) risk = RiskLevel.Medium;
            t = t.Replace("---RISK:HIGH---", "").Replace("---RISK:MEDIUM---", "").Replace("---RISK:LOW---", "").Trim();
            var parts = t.Split("---ACTIONS---", 2, StringSplitOptions.TrimEntries);
            summary = parts[0]; recommendation = parts.Length > 1 ? parts[1] : null;
            status = AiInsightStatus.Reviewed;
        }
        else
        {
            summary = $"[AI không khả dụng] {result.ErrorMessage}\nDữ liệu: {ctx.OpCount} yêu cầu, {ctx.Overdue} quá hạn, {ctx.PendingApproval} chờ duyệt.";
            recommendation = LocalFallback(ctx); risk = ctx.Overdue > 3 ? RiskLevel.High : ctx.Overdue > 1 ? RiskLevel.Medium : RiskLevel.Low;
        }

        if (summary.Length > 2000) summary = summary[..2000];
        if (recommendation?.Length > 4000) recommendation = recommendation[..4000];

        var insight = new AiInsight { TenantId = tid, ContextType = vm.ContextType, ContextId = vm.ContextId, Question = vm.Question, Summary = summary, Recommendation = recommendation, RiskLevel = risk, Status = status, AskedByUserId = tenant.UserId, CreatedByUserId = tenant.UserId, RawResponseJson = result.RawJson, CreatedAt = DateTimeOffset.UtcNow };
        db.AiInsights.Add(insight);
        await audit.LogAsync("AiInsight", insight.Id, "AiQuery",
            newValueObj: new { Model = result.ModelName ?? "local", InputTokens = result.InputTokens, OutputTokens = result.OutputTokens, Risk = risk });
        await db.SaveChangesAsync();

        return new AiInsightListItem { Id = insight.Id, ContextType = insight.ContextType, Question = insight.Question, Summary = summary, Recommendation = recommendation, RiskLevel = risk.ToString(), Status = status.ToString(), ModelName = result.ModelName, CreatedAt = insight.CreatedAt };
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await db.AiInsights.FindAsync(id);
        if (item != null && item.TenantId == tenant.TenantId) { item.IsDeleted = true; await db.SaveChangesAsync(); }
    }

    public async Task<AiRecommendationsViewModel> GenerateRecommendationsAsync()
    {
        var tid = tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var ctx = await BuildContextAsync(tid, today);
        var items = new List<AiRecommendationItem>();
        var now = DateTimeOffset.UtcNow;

        // ── OPERATIONS ────────────────────────────────────
        if (ctx.Overdue > 0) items.Add(new() { Id = Guid.NewGuid(), Category = "Operations", Title = $"⚠️ {ctx.Overdue} yêu cầu vận hành quá hạn", Description = $"Có {ctx.Overdue} yêu cầu đã quá hạn xử lý. Cần ưu tiên giải quyết để tránh ảnh hưởng đến hiệu suất vận hành.", Priority = ctx.Overdue > 5 ? "Critical" : "High", Icon = "fa-fire", ActionUrl = "/OperationRequest", CreatedAt = now });
        if (ctx.PendingApproval > 5) items.Add(new() { Id = Guid.NewGuid(), Category = "Operations", Title = $"📋 {ctx.PendingApproval} phê duyệt chờ xử lý", Description = $"Tích đọng {ctx.PendingApproval} phê duyệt chờ. Nên phân quyền hoặc ủy quyền để giảm thời gian chờ.", Priority = ctx.PendingApproval > 15 ? "High" : "Normal", Icon = "fa-clock", ActionUrl = "/Approval", CreatedAt = now });
        if (ctx.OpCount > 0 && ctx.CompletedMonth == 0) items.Add(new() { Id = Guid.NewGuid(), Category = "Operations", Title = "📊 Chưa hoàn thành yêu cầu nào trong tháng", Description = "Tháng này chưa có yêu cầu nào được hoàn thành. Cần đánh giá lại quy trình xử lý.", Priority = "High", Icon = "fa-chart-line", ActionUrl = "/Reports/Dashboard", CreatedAt = now });

        // ── FINANCE ───────────────────────────────────────
        if (ctx.BudgetPlan > 0) {
            var pct = ctx.BudgetUsed / ctx.BudgetPlan * 100;
            if (pct > 90) items.Add(new() { Id = Guid.NewGuid(), Category = "Finance", Title = $"🔴 Ngân sách đã dùng {pct:F0}%", Description = $"Ngân sách kế hoạch {ctx.BudgetPlan:N0}₫, đã chi {ctx.BudgetUsed:N0}₫. Cần kiểm soát chi tiêu ngay.", Priority = "Critical", Icon = "fa-exclamation-circle", ActionUrl = "/Reports/Finance", CreatedAt = now });
            else if (pct > 70) items.Add(new() { Id = Guid.NewGuid(), Category = "Finance", Title = $"🟡 Ngân sách đã dùng {pct:F0}%", Description = $"Ngân sách đang ở mức cao. Cần lập kế hoạch kiểm soát chi phí cho các tháng còn lại.", Priority = "High", Icon = "fa-wallet", ActionUrl = "/Reports/Finance", CreatedAt = now });
        }
        if (ctx.PendingPay > 0) items.Add(new() { Id = Guid.NewGuid(), Category = "Finance", Title = $"💳 {ctx.PendingPay} thanh toán chờ ({ctx.PendingPayAmt:N0}₫)", Description = "Có thanh toán chờ duyệt. Chậm trễ có thể ảnh hưởng quan hệ với nhà cung cấp.", Priority = ctx.PendingPayAmt > 100_000_000 ? "High" : "Normal", Icon = "fa-credit-card", ActionUrl = "/PaymentRequest", CreatedAt = now });

        // ── CASH FLOW ─────────────────────────────────────
        var cashBal = ctx.CashIncome - ctx.CashExpense;
        if (cashBal < 0) items.Add(new() { Id = Guid.NewGuid(), Category = "CashFlow", Title = $"🔴 Dòng tiền âm: {cashBal:N0}₫", Description = $"Chi vượt thu {Math.Abs(cashBal):N0}₫. Cần tăng thu hoặc cắt giảm chi phí không cần thiết.", Priority = "Critical", Icon = "fa-money-bill-transfer", ActionUrl = "/Reports/CashFlow", CreatedAt = now });
        if (ctx.CashPending > 3) items.Add(new() { Id = Guid.NewGuid(), Category = "CashFlow", Title = $"💸 {ctx.CashPending} giao dịch thu chi chờ duyệt", Description = "Nhiều giao dịch chờ duyệt có thể gây sai lệch báo cáo tài chính.", Priority = "Normal", Icon = "fa-receipt", ActionUrl = "/CashBook", CreatedAt = now });

        // ── INVENTORY ─────────────────────────────────────
        if (ctx.StockAlerts > 0) items.Add(new() { Id = Guid.NewGuid(), Category = "Inventory", Title = $"📦 {ctx.StockAlerts} cảnh báo tồn kho", Description = "Sản phẩm dưới mức tồn kho an toàn. Cần lập đơn đặt hàng bổ sung.", Priority = ctx.StockAlerts > 5 ? "High" : "Normal", Icon = "fa-cubes", ActionUrl = "/Inventory", CreatedAt = now });
        if (ctx.GRCount == 0 && ctx.ProductCount > 0) items.Add(new() { Id = Guid.NewGuid(), Category = "Inventory", Title = "📥 Chưa có phiếu nhập kho", Description = "Hệ thống chưa ghi nhận phiếu nhập kho nào. Cần kiểm tra quy trình nhập hàng.", Priority = "Normal", Icon = "fa-warehouse", ActionUrl = "/GoodsReceipt", CreatedAt = now });

        // ── CRM / SALES ───────────────────────────────────
        if (ctx.Opportunities > 0) {
            var wr = (double)ctx.OppWon / ctx.Opportunities * 100;
            if (wr < 20 && ctx.OppWon + ctx.OppLost >= 5) items.Add(new() { Id = Guid.NewGuid(), Category = "CRM", Title = $"🤝 Win rate thấp: {wr:F0}%", Description = $"Win rate chỉ {wr:F0}% ({ctx.OppWon}/{ctx.Opportunities}). Cần đánh giá lại quy trình bán hàng và chất lượng lead.", Priority = "High", Icon = "fa-handshake", ActionUrl = "/Reports/Crm", CreatedAt = now });
        }
        if (ctx.InteractionCount == 0 && ctx.Customers > 0) items.Add(new() { Id = Guid.NewGuid(), Category = "CRM", Title = "📞 Chưa có tương tác khách hàng", Description = "Chưa ghi nhận tương tác CRM nào. Cần thiết lập quy trình chăm sóc khách hàng.", Priority = "Normal", Icon = "fa-comments", ActionUrl = "/CrmInteraction", CreatedAt = now });

        // ── HR ────────────────────────────────────────────
        if (ctx.PendingLeaves > 3) items.Add(new() { Id = Guid.NewGuid(), Category = "HR", Title = $"🏖️ {ctx.PendingLeaves} đơn nghỉ phép chờ duyệt", Description = "Nhiều đơn nghỉ phép chưa xử lý. Cần duyệt kịp thời để đảm bảo lịch công tác.", Priority = ctx.PendingLeaves > 10 ? "High" : "Normal", Icon = "fa-umbrella-beach", ActionUrl = "/LeaveRequest", CreatedAt = now });
        if (ctx.Depts > 0 && ctx.DeptHeadcounts.Any()) {
            var maxDept = ctx.DeptHeadcounts.OrderByDescending(d => d.Value).First();
            var minDept = ctx.DeptHeadcounts.OrderBy(d => d.Value).First();
            if (maxDept.Value > minDept.Value * 3 && minDept.Value > 0) items.Add(new() { Id = Guid.NewGuid(), Category = "HR", Title = "⚖️ Mất cân đối nhân sự giữa phòng ban", Description = $"Phòng {maxDept.Key} ({maxDept.Value} NV) gấp {maxDept.Value/minDept.Value}x phòng {minDept.Key} ({minDept.Value} NV). Cần đánh giá phân bổ nhân sự.", Priority = "Normal", Icon = "fa-users", ActionUrl = "/Reports/Hr", CreatedAt = now });
        }

        // ── KPI/OKR ───────────────────────────────────────
        if (ctx.OkrKR > 0 && ctx.OkrAvg < 30) items.Add(new() { Id = Guid.NewGuid(), Category = "KPI", Title = $"🎯 OKR tiến độ thấp: {ctx.OkrAvg:F0}%", Description = $"Tiến độ OKR trung bình chỉ {ctx.OkrAvg:F0}%. Cần rà soát và điều chỉnh mục tiêu hoặc tăng cường nguồn lực.", Priority = ctx.OkrAvg < 15 ? "High" : "Normal", Icon = "fa-bullseye", ActionUrl = "/Reports/KpiOkr", CreatedAt = now });

        // ── PROCUREMENT ───────────────────────────────────
        if (ctx.ProcPending > 3) items.Add(new() { Id = Guid.NewGuid(), Category = "Operations", Title = $"🛒 {ctx.ProcPending} đề xuất mua sắm chờ duyệt", Description = "Nhiều đề xuất mua sắm đang chờ. Chậm trễ có thể ảnh hưởng hoạt động sản xuất.", Priority = ctx.ProcPending > 10 ? "High" : "Normal", Icon = "fa-cart-shopping", ActionUrl = "/Procurement", CreatedAt = now });

        // Sort by priority
        var priorityOrder = new Dictionary<string, int> { ["Critical"] = 0, ["High"] = 1, ["Normal"] = 2, ["Low"] = 3 };
        items = items.OrderBy(i => priorityOrder.GetValueOrDefault(i.Priority, 9)).ToList();

        return new AiRecommendationsViewModel
        {
            Items = items,
            CriticalCount = items.Count(i => i.Priority == "Critical"),
            HighCount = items.Count(i => i.Priority == "High"),
            NormalCount = items.Count(i => i.Priority == "Normal"),
            TotalNew = items.Count,
            GeneratedAt = now
        };
    }

    public async Task<List<AiQuickAction>> GetQuickActionsAsync()
    {
        var tid = tenant.TenantId; var today = DateOnly.FromDateTime(DateTime.Today);
        var actions = new List<AiQuickAction>();
        var overdue = await db.OperationRequests.CountAsync(r => r.TenantId == tid && !r.IsDeleted && r.DueDate < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled);
        if (overdue > 0) actions.Add(new AiQuickAction { Icon = "fa-fire", Label = $"Phân tích {overdue} yêu cầu quá hạn", Question = $"Phân tích chi tiết {overdue} yêu cầu vận hành quá hạn, nguyên nhân và đề xuất giải pháp", ContextType = "Operations", Urgency = "high" });
        var budgetUsed = await db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == today.Year).SumAsync(e => e.Amount);
        var budgetPlan = await db.Budgets.Where(b => b.TenantId == tid && !b.IsDeleted && b.FiscalYear == today.Year).SumAsync(b => b.PlannedAmount);
        if (budgetPlan > 0) actions.Add(new AiQuickAction { Icon = "fa-chart-pie", Label = "Phân tích tài chính", Question = "Phân tích sức khỏe tài chính: ngân sách, chi phí, thanh toán. Đề xuất tối ưu chi tiêu", ContextType = "Finance", Urgency = budgetUsed / budgetPlan > 0.8m ? "high" : "normal" });
        actions.Add(new AiQuickAction { Icon = "fa-users", Label = "Đánh giá nhân sự", Question = "Phân tích cơ cấu nhân sự theo phòng ban và đề xuất tối ưu", ContextType = "HR", Urgency = "normal" });
        actions.Add(new AiQuickAction { Icon = "fa-bullseye", Label = "Tiến độ KPI/OKR", Question = "Đánh giá tiến độ KPI/OKR, xác định mục tiêu chậm và đề xuất hành động", ContextType = "KPI", Urgency = "normal" });
        var pending = await db.ApprovalTasks.CountAsync(t => t.TenantId == tid && t.Status == ApprovalStatus.Pending && !t.IsDeleted);
        if (pending > 3) actions.Add(new AiQuickAction { Icon = "fa-clock", Label = $"Tối ưu {pending} phê duyệt chờ", Question = $"Có {pending} phê duyệt chờ xử lý. Phân tích ảnh hưởng và đề xuất xử lý nhanh hơn", ContextType = "Approval", Urgency = pending > 10 ? "high" : "normal" });
        actions.Add(new AiQuickAction { Icon = "fa-lightbulb", Label = "Báo cáo tổng quan CEO", Question = "Tạo báo cáo cho Ban GĐ: vận hành, tài chính, nhân sự, KPI/OKR, rủi ro và đề xuất chiến lược", ContextType = "Executive", Urgency = "normal" });
        var stockAlerts = await db.StockAlerts.CountAsync(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active);
        actions.Add(new AiQuickAction { Icon = "fa-cubes", Label = "Phân tích kho vận", Question = "Phân tích tồn kho, xu hướng nhập/xuất kho, cảnh báo tồn kho thấp và đề xuất tối ưu", ContextType = "Inventory", Urgency = stockAlerts > 0 ? "high" : "normal" });
        var cashBal = await db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted && t.Status != CashTransactionStatus.Voided).SumAsync(t => t.TransactionType == "Income" ? t.Amount : -t.Amount);
        actions.Add(new AiQuickAction { Icon = "fa-money-bill-transfer", Label = "Phân tích dòng tiền", Question = "Phân tích dòng tiền thu chi, cân đối tài chính và dự báo xu hướng", ContextType = "CashFlow", Urgency = cashBal < 0 ? "high" : "normal" });
        actions.Add(new AiQuickAction { Icon = "fa-handshake", Label = "Phân tích CRM & Bán hàng", Question = "Phân tích pipeline bán hàng, win rate, top khách hàng và chiến lược tăng trưởng doanh thu", ContextType = "CRM", Urgency = "normal" });
        actions.Add(new AiQuickAction { Icon = "fa-chart-bar", Label = "Tổng hợp hiệu quả tháng", Question = "Báo cáo tổng hợp hiệu quả kinh doanh tháng này: vận hành, tài chính, kho, bán hàng, nhân sự", ContextType = "Dashboard", Urgency = "normal" });
        return actions;
    }

    // ── Data collection ──────────────────────────────────────────────────────
    record BizCtx(int OpCount, int Overdue, int CompletedMonth, int PendingApproval, int Employees, int Depts, List<KeyValuePair<string,int>> DeptHeadcounts, decimal BudgetPlan, decimal BudgetUsed, int ActiveBudgets, decimal ExpenseMonth, int ProcDraft, int ProcPending, int POCount, int Customers, int Vendors, int Products, int KpiCount, int OkrObj, int OkrKR, double OkrAvg, int PendingPay, decimal PendingPayAmt,
        // Inventory
        int GRCount, int GICount, int StockAlerts, int ProductCount,
        // CashFlow
        decimal CashIncome, decimal CashExpense, int CashPending, int CashTxnCount,
        // CRM/Sales
        int Opportunities, int OppWon, int OppLost, decimal PipelineValue, decimal WonValue, int InteractionCount,
        // Leave
        int PendingLeaves, int LeaveThisMonth);

    async Task<BizCtx> BuildContextAsync(Guid tid, DateOnly today)
    {
        var som = new DateOnly(today.Year, today.Month, 1);
        var somDto = new DateTimeOffset(som.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return new BizCtx(
            await db.OperationRequests.CountAsync(r => r.TenantId == tid && !r.IsDeleted),
            await db.OperationRequests.CountAsync(r => r.TenantId == tid && !r.IsDeleted && r.DueDate < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled),
            await db.OperationRequests.CountAsync(r => r.TenantId == tid && !r.IsDeleted && r.Status == OperationStatus.Completed && r.UpdatedAt.HasValue && r.UpdatedAt.Value >= somDto),
            await db.ApprovalTasks.CountAsync(t => t.TenantId == tid && t.Status == ApprovalStatus.Pending && !t.IsDeleted),
            await db.AppUsers.CountAsync(u => u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted),
            await db.OrganizationUnits.CountAsync(o => o.TenantId == tid && o.IsActive && !o.IsDeleted),
            await db.AppUsers.Where(u => u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted && u.OrganizationUnitId.HasValue).GroupBy(u => u.OrganizationUnit!.Name).Select(g => new KeyValuePair<string,int>(g.Key, g.Count())).ToListAsync(),
            await db.Budgets.Where(b => b.TenantId == tid && !b.IsDeleted && b.FiscalYear == today.Year).SumAsync(b => b.PlannedAmount),
            await db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == today.Year).SumAsync(e => e.Amount),
            await db.Budgets.CountAsync(b => b.TenantId == tid && !b.IsDeleted && b.Status == BudgetStatus.Active),
            await db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate >= som).SumAsync(e => e.Amount),
            await db.ProcurementRequests.CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.Status == ProcurementStatus.Draft),
            await db.ProcurementRequests.CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.Status == ProcurementStatus.Submitted),
            await db.PurchaseOrders.CountAsync(po => po.TenantId == tid && !po.IsDeleted),
            await db.Customers.CountAsync(c => c.TenantId == tid && c.IsActive && !c.IsDeleted),
            await db.Vendors.CountAsync(v => v.TenantId == tid && v.IsActive && !v.IsDeleted),
            await db.ProductServices.CountAsync(p => p.TenantId == tid && p.IsActive && !p.IsDeleted),
            await db.KpiDefinitions.CountAsync(k => k.TenantId == tid && !k.IsDeleted),
            await db.OkrObjectives.CountAsync(o => o.TenantId == tid && !o.IsDeleted),
            await db.OkrKeyResults.CountAsync(k => k.TenantId == tid && !k.IsDeleted),
            await db.OkrKeyResults.Where(k => k.TenantId == tid && !k.IsDeleted && k.TargetValue > 0).Select(k => (double)(k.IsInverse ? (k.TargetValue - k.CurrentValue) / k.TargetValue * 100 : k.CurrentValue / k.TargetValue * 100)).DefaultIfEmpty(0).AverageAsync(),
            await db.PaymentRequests.CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.Status == PaymentStatus.Submitted),
            await db.PaymentRequests.Where(p => p.TenantId == tid && !p.IsDeleted && p.Status == PaymentStatus.Submitted).SumAsync(p => p.TotalAmount),
            // Inventory
            await db.GoodsReceipts.CountAsync(r => r.TenantId == tid && !r.IsDeleted),
            await db.GoodsIssues.CountAsync(i => i.TenantId == tid && !i.IsDeleted),
            await db.StockAlerts.CountAsync(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active),
            await db.ProductServices.CountAsync(p => p.TenantId == tid && p.IsActive && !p.IsDeleted && p.Type == "Product"),
            // CashFlow
            await db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted && t.TransactionType == "Income" && t.Status != CashTransactionStatus.Voided).SumAsync(t => t.Amount),
            await db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted && t.TransactionType == "Expense" && t.Status != CashTransactionStatus.Voided).SumAsync(t => t.Amount),
            await db.CashTransactions.CountAsync(t => t.TenantId == tid && !t.IsDeleted && t.Status == CashTransactionStatus.Recorded),
            await db.CashTransactions.CountAsync(t => t.TenantId == tid && !t.IsDeleted),
            // CRM/Sales
            await db.SalesOpportunities.CountAsync(o => o.TenantId == tid && !o.IsDeleted),
            await db.SalesOpportunities.CountAsync(o => o.TenantId == tid && !o.IsDeleted && o.Stage == "ClosedWon"),
            await db.SalesOpportunities.CountAsync(o => o.TenantId == tid && !o.IsDeleted && o.Stage == "ClosedLost"),
            await db.SalesOpportunities.Where(o => o.TenantId == tid && !o.IsDeleted && o.Stage != "ClosedWon" && o.Stage != "ClosedLost").SumAsync(o => o.EstimatedValue),
            await db.SalesOpportunities.Where(o => o.TenantId == tid && !o.IsDeleted && o.Stage == "ClosedWon").SumAsync(o => o.EstimatedValue),
            await db.CrmInteractions.CountAsync(i => i.TenantId == tid && !i.IsDeleted),
            // Leave
            await db.LeaveRequests.CountAsync(l => l.TenantId == tid && !l.IsDeleted && l.Status == LeaveStatus.Submitted),
            await db.LeaveRequests.CountAsync(l => l.TenantId == tid && !l.IsDeleted && l.StartDate >= som && l.StartDate <= new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)))
        );
    }

    // ── Prompts ──────────────────────────────────────────────────────────────
    static string SystemPrompt() => "Bạn là AI Copilot cho hệ thống quản lý doanh nghiệp OmniBizAI. Bạn đóng vai cố vấn quản trị kinh doanh thông minh.\n\nQuy tắc:\n1. Trả lời TIẾNG VIỆT, chuyên nghiệp, thực tiễn\n2. Dựa trên DỮ LIỆU THỰC từ hệ thống, không bịa\n3. Đề xuất CỤ THỂ, KHẢ THI, có thời hạn\n4. Dùng emoji: ⚠️📊✅💡🔴🟡🟢📦💰🎯\n5. Cảnh báo rủi ro rõ ràng với mức độ\n6. Mỗi đề xuất hành động phải có: ưu tiên (🔴Cao/🟡TB/🟢Thấp), người chịu trách nhiệm gợi ý, thời hạn gợi ý\n\nĐịnh dạng:\nPHẦN 1 (trước ---ACTIONS---): Tóm tắt phân tích tình hình\nPHẦN 2 (sau ---ACTIONS---): Danh sách đề xuất hành động cụ thể, đánh số, mỗi hành động một dòng\nCuối: ---RISK:LOW--- hoặc ---RISK:MEDIUM--- hoặc ---RISK:HIGH---";

    static string UserPrompt(AiInsightCreateViewModel vm, BizCtx c)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"## CÂU HỎI: {vm.Question}");
        sb.AppendLine($"## NGỮ CẢNH: {vm.ContextType}\n");
        sb.AppendLine("## DỮ LIỆU HỆ THỐNG:");
        sb.AppendLine($"### VẬN HÀNH: {c.OpCount} yêu cầu, {c.Overdue} quá hạn, {c.CompletedMonth} hoàn thành tháng này, {c.PendingApproval} chờ duyệt");
        sb.AppendLine($"### NHÂN SỰ: {c.Employees} NV hoạt động, {c.Depts} phòng ban");
        if (c.DeptHeadcounts.Any()) { sb.AppendLine("Phân bổ: " + string.Join(", ", c.DeptHeadcounts.OrderByDescending(d => d.Value).Take(8).Select(d => $"{d.Key}:{d.Value}"))); }
        sb.AppendLine($"### TÀI CHÍNH: NS kế hoạch {c.BudgetPlan:N0}₫, đã chi {c.BudgetUsed:N0}₫ ({(c.BudgetPlan > 0 ? c.BudgetUsed/c.BudgetPlan*100 : 0):F1}%), chi tháng {c.ExpenseMonth:N0}₫, {c.ActiveBudgets} NS hoạt động, {c.PendingPay} thanh toán chờ ({c.PendingPayAmt:N0}₫)");
        sb.AppendLine($"### THU CHI: Thu {c.CashIncome:N0}₫, Chi {c.CashExpense:N0}₫, Số dư {c.CashIncome - c.CashExpense:N0}₫, {c.CashTxnCount} giao dịch, {c.CashPending} chờ duyệt");
        sb.AppendLine($"### MUA SẮM: {c.ProcDraft} nháp, {c.ProcPending} chờ duyệt, {c.POCount} PO");
        sb.AppendLine($"### KHO VẬN: {c.ProductCount} sản phẩm, {c.GRCount} phiếu nhập, {c.GICount} phiếu xuất, {c.StockAlerts} cảnh báo tồn kho");
        var winRate = c.Opportunities > 0 ? (double)c.OppWon / c.Opportunities * 100 : 0;
        sb.AppendLine($"### CRM/BÁN HÀNG: {c.Customers} KH, {c.Vendors} NCC, {c.Products} SP/DV, {c.Opportunities} cơ hội ({c.OppWon} thắng, {c.OppLost} thua, Win rate {winRate:F0}%), Pipeline {c.PipelineValue:N0}₫, Doanh thu Won {c.WonValue:N0}₫, {c.InteractionCount} tương tác KH");
        sb.AppendLine($"### KPI/OKR: {c.KpiCount} KPI, {c.OkrObj} mục tiêu OKR, {c.OkrKR} kết quả then chốt, tiến độ TB: {c.OkrAvg:F1}%");
        sb.AppendLine($"### NGHỈ PHÉP: {c.PendingLeaves} đơn chờ duyệt, {c.LeaveThisMonth} nghỉ phép tháng này");
        return sb.ToString();
    }

    static string LocalFallback(BizCtx c)
    {
        var items = new List<string>();
        if (c.Overdue > 0) items.Add($"⚠️ Ưu tiên xử lý {c.Overdue} yêu cầu quá hạn");
        if (c.PendingApproval > 5) items.Add($"📋 {c.PendingApproval} phê duyệt chờ xử lý");
        if (c.BudgetPlan > 0 && c.BudgetUsed / c.BudgetPlan > 0.8m) items.Add($"💰 Ngân sách đã dùng {c.BudgetUsed/c.BudgetPlan*100:F0}%");
        if (c.ProcPending > 0) items.Add($"🛒 {c.ProcPending} đề xuất mua sắm chờ duyệt");
        if (c.PendingPay > 0) items.Add($"💳 {c.PendingPay} thanh toán chờ ({c.PendingPayAmt:N0}₫)");
        if (c.StockAlerts > 0) items.Add($"📦 {c.StockAlerts} cảnh báo tồn kho cần xử lý");
        if (c.CashIncome - c.CashExpense < 0) items.Add($"🔴 Dòng tiền âm: {c.CashIncome - c.CashExpense:N0}₫");
        if (c.CashPending > 0) items.Add($"💸 {c.CashPending} giao dịch thu chi chờ duyệt");
        if (c.PendingLeaves > 3) items.Add($"🏖️ {c.PendingLeaves} đơn nghỉ phép chờ duyệt");
        if (c.OkrAvg < 30 && c.OkrKR > 0) items.Add($"🎯 OKR chỉ {c.OkrAvg:F0}%");
        if (!items.Any()) items.Add("✅ Hệ thống ổn định");
        return string.Join("\n", items);
    }
}

public class AiQuickAction { public string Icon { get; set; } = ""; public string Label { get; set; } = ""; public string Question { get; set; } = ""; public string ContextType { get; set; } = ""; public string Urgency { get; set; } = "normal"; }

// ═════════════════════════════════════════════════════════════════════════════
// ANOMALY DETECTION SERVICE
// ═════════════════════════════════════════════════════════════════════════════
public class AnomalyDetectionService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<AnomalyDashboardViewModel> ScanAsync(string? moduleFilter = null, string? severityFilter = null)
    {
        var tid = tenant.TenantId;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var som = new DateOnly(today.Year, today.Month, 1);
        var now = DateTimeOffset.UtcNow;
        var alerts = new List<AnomalyAlertItem>();
        int idx = 0;

        // ── OPERATIONS ────────────────────────────────────
        var overdue = await db.OperationRequests.CountAsync(r => r.TenantId == tid && !r.IsDeleted && r.DueDate < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled);
        if (overdue > 0) alerts.Add(new() { Id = $"OP-{++idx}", Module = "Operations", Severity = overdue > 5 ? "Critical" : "Warning", Title = $"{overdue} yêu cầu vận hành quá hạn", Description = $"Có {overdue} yêu cầu đã quá hạn chưa hoàn thành. Ảnh hưởng đến SLA và hiệu suất.", Icon = "fa-fire", MetricValue = overdue.ToString(), ThresholdValue = "0", ActionUrl = "/OperationRequest" });

        var pendingApproval = await db.ApprovalTasks.CountAsync(t => t.TenantId == tid && t.Status == ApprovalStatus.Pending && !t.IsDeleted);
        if (pendingApproval > 10) alerts.Add(new() { Id = $"OP-{++idx}", Module = "Operations", Severity = pendingApproval > 20 ? "Critical" : "Warning", Title = $"{pendingApproval} phê duyệt tắc nghẽn", Description = "Quá nhiều phê duyệt chờ xử lý gây trì hoãn quy trình kinh doanh.", Icon = "fa-clock", MetricValue = pendingApproval.ToString(), ThresholdValue = "10", ActionUrl = "/Approval" });

        // ── FINANCE ───────────────────────────────────────
        var budgetPlan = await db.Budgets.Where(b => b.TenantId == tid && !b.IsDeleted && b.FiscalYear == today.Year).SumAsync(b => b.PlannedAmount);
        var budgetUsed = await db.Expenses.Where(e => e.TenantId == tid && !e.IsDeleted && e.ExpenseDate.Year == today.Year).SumAsync(e => e.Amount);
        if (budgetPlan > 0) {
            var pct = budgetUsed / budgetPlan * 100;
            if (pct > 90) alerts.Add(new() { Id = $"FI-{++idx}", Module = "Finance", Severity = "Critical", Title = $"Ngân sách đã dùng {pct:F0}%", Description = $"Ngân sách năm {today.Year}: kế hoạch {budgetPlan:N0}₫, đã chi {budgetUsed:N0}₫. Gần hết ngân sách.", Icon = "fa-chart-pie", MetricValue = $"{pct:F0}%", ThresholdValue = "90%", ActionUrl = "/Reports/Finance" });
            else if (pct > 75) alerts.Add(new() { Id = $"FI-{++idx}", Module = "Finance", Severity = "Warning", Title = $"Ngân sách đã dùng {pct:F0}%", Description = $"Chi phí đang tăng nhanh. Cần lập kế hoạch kiểm soát.", Icon = "fa-wallet", MetricValue = $"{pct:F0}%", ThresholdValue = "75%", ActionUrl = "/Reports/Finance" });
        }

        var pendingPay = await db.PaymentRequests.CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.Status == PaymentStatus.Submitted);
        var pendingPayAmt = await db.PaymentRequests.Where(p => p.TenantId == tid && !p.IsDeleted && p.Status == PaymentStatus.Submitted).SumAsync(p => p.TotalAmount);
        if (pendingPayAmt > 50_000_000) alerts.Add(new() { Id = $"FI-{++idx}", Module = "Finance", Severity = pendingPayAmt > 200_000_000 ? "Critical" : "Warning", Title = $"{pendingPay} thanh toán chờ ({pendingPayAmt:N0}₫)", Description = "Giá trị thanh toán tồn đọng cao. Có thể ảnh hưởng đến quan hệ NCC.", Icon = "fa-credit-card", MetricValue = $"{pendingPayAmt:N0}₫", ThresholdValue = "50,000,000₫", ActionUrl = "/PaymentRequest" });

        // ── CASH FLOW ─────────────────────────────────────
        var cashIn = await db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted && t.TransactionType == "Income" && t.Status != CashTransactionStatus.Voided).SumAsync(t => t.Amount);
        var cashOut = await db.CashTransactions.Where(t => t.TenantId == tid && !t.IsDeleted && t.TransactionType == "Expense" && t.Status != CashTransactionStatus.Voided).SumAsync(t => t.Amount);
        if (cashIn - cashOut < 0) alerts.Add(new() { Id = $"CF-{++idx}", Module = "CashFlow", Severity = "Critical", Title = $"Dòng tiền âm: {cashIn - cashOut:N0}₫", Description = $"Thu {cashIn:N0}₫ < Chi {cashOut:N0}₫. Doanh nghiệp đang chi vượt thu.", Icon = "fa-money-bill-transfer", MetricValue = $"{cashIn - cashOut:N0}₫", ThresholdValue = "> 0₫", ActionUrl = "/Reports/CashFlow" });

        var cashPending = await db.CashTransactions.CountAsync(t => t.TenantId == tid && !t.IsDeleted && t.Status == CashTransactionStatus.Recorded);
        if (cashPending > 5) alerts.Add(new() { Id = $"CF-{++idx}", Module = "CashFlow", Severity = "Warning", Title = $"{cashPending} giao dịch chờ duyệt", Description = "Nhiều giao dịch thu chi chờ duyệt. Có thể gây sai lệch số liệu.", Icon = "fa-receipt", MetricValue = cashPending.ToString(), ThresholdValue = "5", ActionUrl = "/CashBook" });

        // ── INVENTORY ─────────────────────────────────────
        var stockAlertCount = await db.StockAlerts.CountAsync(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active);
        var criticalStock = await db.StockAlerts.CountAsync(a => a.TenantId == tid && !a.IsDeleted && a.Status == StockAlertStatus.Active && a.AlertType == "Critical");
        if (criticalStock > 0) alerts.Add(new() { Id = $"INV-{++idx}", Module = "Inventory", Severity = "Critical", Title = $"{criticalStock} sản phẩm tồn kho nguy hiểm", Description = "Sản phẩm dưới mức an toàn. Cần nhập hàng khẩn cấp.", Icon = "fa-triangle-exclamation", MetricValue = criticalStock.ToString(), ThresholdValue = "0", ActionUrl = "/Inventory" });
        else if (stockAlertCount > 0) alerts.Add(new() { Id = $"INV-{++idx}", Module = "Inventory", Severity = "Warning", Title = $"{stockAlertCount} cảnh báo tồn kho", Description = "Có sản phẩm cần chú ý về mức tồn kho.", Icon = "fa-cubes", MetricValue = stockAlertCount.ToString(), ThresholdValue = "0", ActionUrl = "/Inventory" });

        // ── CRM ───────────────────────────────────────────
        var oppTotal = await db.SalesOpportunities.CountAsync(o => o.TenantId == tid && !o.IsDeleted);
        var oppWon = await db.SalesOpportunities.CountAsync(o => o.TenantId == tid && !o.IsDeleted && o.Stage == "ClosedWon");
        var oppLost = await db.SalesOpportunities.CountAsync(o => o.TenantId == tid && !o.IsDeleted && o.Stage == "ClosedLost");
        if (oppWon + oppLost >= 5) {
            var wr = (double)oppWon / (oppWon + oppLost) * 100;
            if (wr < 25) alerts.Add(new() { Id = $"CRM-{++idx}", Module = "CRM", Severity = "Warning", Title = $"Win rate thấp: {wr:F0}%", Description = $"Tỷ lệ thắng {oppWon}/{oppWon + oppLost}. Cần cải thiện quy trình bán hàng.", Icon = "fa-handshake", MetricValue = $"{wr:F0}%", ThresholdValue = "25%", ActionUrl = "/Reports/Crm" });
        }

        var staleOpps = await db.SalesOpportunities.CountAsync(o => o.TenantId == tid && !o.IsDeleted && o.Stage != "ClosedWon" && o.Stage != "ClosedLost" && o.ExpectedCloseDate.HasValue && o.ExpectedCloseDate < today);
        if (staleOpps > 0) alerts.Add(new() { Id = $"CRM-{++idx}", Module = "CRM", Severity = staleOpps > 3 ? "Warning" : "Info", Title = $"{staleOpps} cơ hội quá hạn chốt", Description = "Cơ hội bán hàng quá ngày dự kiến chốt. Cần cập nhật hoặc đóng.", Icon = "fa-hourglass-end", MetricValue = staleOpps.ToString(), ThresholdValue = "0", ActionUrl = "/SalesOpportunity" });

        // ── HR ────────────────────────────────────────────
        var pendingLeaves = await db.LeaveRequests.CountAsync(l => l.TenantId == tid && !l.IsDeleted && l.Status == LeaveStatus.Submitted);
        if (pendingLeaves > 5) alerts.Add(new() { Id = $"HR-{++idx}", Module = "HR", Severity = pendingLeaves > 15 ? "Warning" : "Info", Title = $"{pendingLeaves} đơn nghỉ phép chờ", Description = "Nhiều đơn nghỉ phép chưa xử lý. Ảnh hưởng tinh thần nhân viên.", Icon = "fa-umbrella-beach", MetricValue = pendingLeaves.ToString(), ThresholdValue = "5", ActionUrl = "/LeaveRequest" });

        // ── PROCUREMENT ───────────────────────────────────
        var procPending = await db.ProcurementRequests.CountAsync(p => p.TenantId == tid && !p.IsDeleted && p.Status == ProcurementStatus.Submitted);
        if (procPending > 5) alerts.Add(new() { Id = $"PROC-{++idx}", Module = "Operations", Severity = procPending > 10 ? "Warning" : "Info", Title = $"{procPending} đề xuất mua sắm tồn đọng", Description = "Đề xuất mua sắm chờ lâu có thể trì hoãn dự án.", Icon = "fa-cart-shopping", MetricValue = procPending.ToString(), ThresholdValue = "5", ActionUrl = "/Procurement" });

        // ── KPI/OKR ───────────────────────────────────────
        var okrAvg = await db.OkrKeyResults.Where(k => k.TenantId == tid && !k.IsDeleted && k.TargetValue > 0).Select(k => (double)(k.IsInverse ? (k.TargetValue - k.CurrentValue) / k.TargetValue * 100 : k.CurrentValue / k.TargetValue * 100)).DefaultIfEmpty(0).AverageAsync();
        var okrCount = await db.OkrKeyResults.CountAsync(k => k.TenantId == tid && !k.IsDeleted);
        if (okrCount > 0 && okrAvg < 25) alerts.Add(new() { Id = $"KPI-{++idx}", Module = "KPI", Severity = okrAvg < 10 ? "Critical" : "Warning", Title = $"OKR tiến độ thấp: {okrAvg:F0}%", Description = $"Tiến độ trung bình {okrAvg:F0}% trên {okrCount} kết quả then chốt. Cần hành động.", Icon = "fa-bullseye", MetricValue = $"{okrAvg:F0}%", ThresholdValue = "25%", ActionUrl = "/Reports/KpiOkr" });

        // Sort: Critical > Warning > Info
        var severityOrder = new Dictionary<string, int> { ["Critical"] = 0, ["Warning"] = 1, ["Info"] = 2 };
        alerts = alerts.OrderBy(a => severityOrder.GetValueOrDefault(a.Severity, 9)).ToList();

        // Apply filters
        var filtered = alerts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(moduleFilter)) filtered = filtered.Where(a => a.Module == moduleFilter);
        if (!string.IsNullOrWhiteSpace(severityFilter)) filtered = filtered.Where(a => a.Severity == severityFilter);

        // Get real stock alerts from DB
        var stockAlerts = await db.StockAlerts.Include(a => a.ProductService).Include(a => a.AcknowledgedByUser)
            .Where(a => a.TenantId == tid && !a.IsDeleted).OrderByDescending(a => a.CreatedAt).Take(20)
            .Select(a => new StockAlertListItem
            {
                Id = a.Id, ProductCode = a.ProductService!.Code, ProductName = a.ProductService.Name,
                AlertType = a.AlertType, CurrentStock = a.CurrentStock, Threshold = a.Threshold,
                Message = a.Message, Status = a.Status.ToString(), CreatedAt = a.CreatedAt,
                AcknowledgedAt = a.AcknowledgedAt, AcknowledgedBy = a.AcknowledgedByUser != null ? a.AcknowledgedByUser.FullName : null
            }).ToListAsync();

        return new AnomalyDashboardViewModel
        {
            Alerts = filtered.ToList(),
            CriticalCount = alerts.Count(a => a.Severity == "Critical"),
            WarningCount = alerts.Count(a => a.Severity == "Warning"),
            InfoCount = alerts.Count(a => a.Severity == "Info"),
            TotalAlerts = alerts.Count,
            ScanTime = now,
            ModuleFilter = moduleFilter,
            SeverityFilter = severityFilter,
            StockAlerts = stockAlerts,
            ActiveStockAlerts = stockAlertCount
        };
    }
}
