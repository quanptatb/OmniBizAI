using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
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
        var today = DateOnly.FromDateTime(DateTime.Today);

        var vm = new DashboardViewModel
        {
            UserFullName = tenant.UserFullName,
            UserRole = tenant.Roles.FirstOrDefault() ?? "",
            TenantName = tenant.TenantName,
            TotalOperationRequests = requests.Count,
            OverdueTasks = requests.Count(r => r.DueDate < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled),
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
}

// ─── OperationRequest ────────────────────────────────────────────────────────
public class OperationRequestService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<OperationRequestListViewModel> GetListAsync(string? search, string? status, string? priority, Guid? deptId, int page = 1)
    {
        var tid = tenant.TenantId;
        var baseQ = db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted);

        var draftCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Draft);
        var submittedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Submitted || r.Status == OperationStatus.InReview);
        var inProgressCount = await baseQ.CountAsync(r => r.Status == OperationStatus.InProgress);
        var completedCount = await baseQ.CountAsync(r => r.Status == OperationStatus.Completed);
        var overdueCount = await baseQ.CountAsync(r => r.DueDate.HasValue && r.DueDate.Value < DateOnly.FromDateTime(DateTime.Today) && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled);

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(r => r.Title.Contains(search) || r.RequestNo.Contains(search));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OperationStatus>(status, out var st)) q = q.Where(r => r.Status == st);
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<PriorityLevel>(priority, out var pr)) q = q.Where(r => r.Priority == pr);
        if (deptId.HasValue) q = q.Where(r => r.OrganizationUnitId == deptId.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * 20).Take(20)
            .Join(db.AppUsers, r => r.RequestedByUserId, u => u.Id, (r, u) => new { r, u })
            .Join(db.OrganizationUnits, x => x.r.OrganizationUnitId, o => o.Id, (x, o) => new OperationRequestListItem
            { Id = x.r.Id, RequestNo = x.r.RequestNo, Title = x.r.Title, Type = x.r.Type, Status = x.r.Status.ToString(), Priority = x.r.Priority.ToString(), Department = o.Name, CreatedBy = x.u.FullName, CreatedAt = x.r.CreatedAt, DueDate = x.r.DueDate, TotalAmount = x.r.TotalAmount })
            .ToListAsync();

        return new OperationRequestListViewModel
        {
            Items = items, TotalCount = total, Page = page,
            DraftCount = draftCount, SubmittedCount = submittedCount, InProgressCount = inProgressCount,
            CompletedCount = completedCount, OverdueCount = overdueCount,
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

        return new OperationRequestDetailViewModel
        {
            Id = r.Id, RequestNo = r.RequestNo, Title = r.Title, Type = r.Type, Status = r.Status.ToString(), Priority = r.Priority.ToString(),
            Department = dept?.Name ?? "", DepartmentId = r.OrganizationUnitId, Customer = customer?.Name, CreatedBy = creator?.FullName ?? "",
            CreatedAt = r.CreatedAt, DueDate = r.DueDate, TotalAmount = r.TotalAmount, Description = r.Description,
            CustomerSiteName = customerSite?.Name,
            Lines = lines, ApprovalTasks = approvals, WorkItems = workItems, AiInsights = aiInsights, ActivityLog = activityLog,
            CanEdit = r.Status is OperationStatus.Draft or OperationStatus.Rejected,
            CanSubmit = r.Status == OperationStatus.Draft,
            CanCancel = r.Status is OperationStatus.Draft or OperationStatus.Submitted,
            CanStartWork = r.Status == OperationStatus.Approved,
            CanComplete = r.Status == OperationStatus.InProgress
        };
    }

    public async Task<Guid> CreateAsync(OperationRequestCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var count = await db.OperationRequests.CountAsync(r => r.TenantId == tid);
        var entity = new OperationRequest
        {
            TenantId = tid, RequestNo = $"OPR-{DateTime.Today.Year}-{count + 1:D3}", Title = vm.Title, Type = vm.Type,
            OrganizationUnitId = vm.OrganizationUnitId, CustomerId = vm.CustomerId, Priority = vm.Priority,
            Status = OperationStatus.Draft, DueDate = vm.DueDate, Description = vm.Description, TotalAmount = vm.TotalAmount,
            RequestedByUserId = tenant.UserId, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.OperationRequests.Add(entity);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Create", EntityName = "OperationRequest", EntityId = entity.Id, NewValuesJson = $"{{\"RequestNo\":\"{entity.RequestNo}\",\"Title\":\"{entity.Title}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<OperationRequestEditViewModel?> GetEditFormAsync(Guid id)
    {
        var r = await db.OperationRequests.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted);
        if (r is null || r.Status is not (OperationStatus.Draft or OperationStatus.Rejected)) return null;

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

        var oldTitle = r.Title;
        r.Title = vm.Title; r.Type = vm.Type; r.OrganizationUnitId = vm.OrganizationUnitId;
        r.CustomerId = vm.CustomerId; r.Priority = vm.Priority; r.DueDate = vm.DueDate;
        r.Description = vm.Description; r.TotalAmount = vm.TotalAmount;
        r.UpdatedAt = DateTimeOffset.UtcNow; r.UpdatedByUserId = tenant.UserId;

        // If rejected, allow resubmission by resetting to Draft
        if (r.Status == OperationStatus.Rejected) r.Status = OperationStatus.Draft;

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Update", EntityName = "OperationRequest", EntityId = r.Id, OldValuesJson = $"{{\"Title\":\"{oldTitle}\"}}", NewValuesJson = $"{{\"Title\":\"{r.Title}\",\"Priority\":\"{r.Priority}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> SubmitAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId || r.Status != OperationStatus.Draft) return false;
        r.Status = OperationStatus.Submitted; r.UpdatedAt = DateTimeOffset.UtcNow;
        db.ApprovalTasks.Add(new ApprovalTask { TenantId = tenant.TenantId, TargetType = "OperationRequest", TargetId = id, StepCode = "DEPARTMENT_REVIEW", AssignedRole = "DEPARTMENT_MANAGER", Status = ApprovalStatus.Pending, CreatedAt = DateTimeOffset.UtcNow });
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Submit", EntityName = "OperationRequest", EntityId = id, OldValuesJson = "{\"Status\":\"Draft\"}", NewValuesJson = "{\"Status\":\"Submitted\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId || r.Status is not (OperationStatus.Draft or OperationStatus.Submitted)) return false;
        r.Status = OperationStatus.Cancelled; r.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Cancel", EntityName = "OperationRequest", EntityId = id, NewValuesJson = "{\"Status\":\"Cancelled\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId) return false;
        r.IsDeleted = true; r.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "OperationRequest", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<OperationRequestCreateViewModel> GetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new OperationRequestCreateViewModel
        {
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted).Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync(),
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted).Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Code + " — " + c.Name }).ToListAsync(),
            Products = await db.ProductServices.Where(p => p.TenantId == tid && p.IsActive && !p.IsDeleted).OrderBy(p => p.Name)
                .Select(p => new SelectOption { Value = p.Id.ToString(), Text = p.Code + " — " + p.Name + (p.StandardPrice.HasValue ? $" ({p.StandardPrice:N0}₫)" : "") }).ToListAsync()
        };
    }

    public async Task<bool> StartWorkAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId || r.Status != OperationStatus.Approved) return false;
        r.Status = OperationStatus.InProgress; r.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "StartWork", EntityName = "OperationRequest", EntityId = id, NewValuesJson = "{\"Status\":\"InProgress\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CompleteAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId || r.Status != OperationStatus.InProgress) return false;
        r.Status = OperationStatus.Completed; r.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Complete", EntityName = "OperationRequest", EntityId = id, NewValuesJson = "{\"Status\":\"Completed\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<Guid> AddLineAsync(Guid requestId, OrderLineInputViewModel input)
    {
        var line = new OperationRequestLine
        {
            TenantId = tenant.TenantId, OperationRequestId = requestId,
            ProductServiceId = input.ProductServiceId, Quantity = input.Quantity,
            UnitPrice = input.UnitPrice, LineAmount = input.Quantity * (input.UnitPrice ?? 0),
            Note = input.Note, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = tenant.UserId
        };
        db.Set<OperationRequestLine>().Add(line);
        // Recalculate total
        var r = await db.OperationRequests.FindAsync(requestId);
        if (r != null)
        {
            var existingTotal = await db.Set<OperationRequestLine>().Where(l => l.OperationRequestId == requestId && !l.IsDeleted).SumAsync(l => l.LineAmount ?? 0);
            r.TotalAmount = existingTotal + (line.LineAmount ?? 0);
        }
        await db.SaveChangesAsync();
        return line.Id;
    }

    public async Task<bool> RemoveLineAsync(Guid lineId)
    {
        var line = await db.Set<OperationRequestLine>().FindAsync(lineId);
        if (line is null || line.TenantId != tenant.TenantId) return false;
        line.IsDeleted = true; line.UpdatedAt = DateTimeOffset.UtcNow;
        // Recalculate total
        var r = await db.OperationRequests.FindAsync(line.OperationRequestId);
        if (r != null)
        {
            r.TotalAmount = await db.Set<OperationRequestLine>().Where(l => l.OperationRequestId == line.OperationRequestId && !l.IsDeleted && l.Id != lineId).SumAsync(l => l.LineAmount ?? 0);
        }
        await db.SaveChangesAsync(); return true;
    }
}


// ─── Work Kanban ─────────────────────────────────────────────────────────────
public class WorkKanbanService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<KanbanBoardViewModel> GetBoardAsync(string? search, Guid? departmentId)
    {
        var tid = tenant.TenantId;
        var query = db.WorkItems
            .AsNoTracking()
            .Include(w => w.OperationRequest)
            .Include(w => w.OrganizationUnit)
            .Include(w => w.Assignments)
                .ThenInclude(a => a.AssignedToUser)
            .Include(w => w.Checklists)
            .Where(w => w.TenantId == tid && !w.IsDeleted);

        if (departmentId.HasValue)
            query = query.Where(w => w.OrganizationUnitId == departmentId.Value);

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

        var columns = GetKanbanColumns();
        var columnsByStatus = columns.ToDictionary(c => c.Status);
        foreach (var item in items)
        {
            if (!columnsByStatus.TryGetValue(item.Status, out var column))
                continue;

            column.Items.Add(new KanbanCardViewModel
            {
                Id = item.Id,
                OperationRequestId = item.OperationRequestId,
                RequestNo = item.OperationRequest?.RequestNo ?? "",
                RequestTitle = item.OperationRequest?.Title ?? "",
                Title = item.Title,
                Description = item.Description,
                Status = item.Status,
                Department = item.OrganizationUnit?.Name ?? "",
                Priority = item.Priority.ToString(),
                PriorityClass = GetPriorityClass(item.Priority),
                AssignedTo = item.Assignments
                    .OrderByDescending(a => a.AssignedAt)
                    .Select(a => a.AssignedToUser?.FullName)
                    .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                DueDate = item.DueDate,
                ChecklistDone = item.Checklists.Count(c => c.IsCompleted && !c.IsDeleted),
                ChecklistTotal = item.Checklists.Count(c => !c.IsDeleted)
            });
        }

        return new KanbanBoardViewModel
        {
            SearchTerm = search,
            DepartmentFilter = departmentId,
            Columns = columns,
            Departments = await GetDepartmentOptionsAsync(tid),
            OperationRequests = await GetOperationRequestOptionsAsync(tid),
            Assignees = await GetAssigneeOptionsAsync(tid),
            CreateForm = new WorkItemCreateViewModel { OrganizationUnitId = departmentId }
        };
    }

    public async Task<(bool Success, string Message)> CreateAsync(WorkItemCreateViewModel input)
    {
        var tid = tenant.TenantId;
        var request = await db.OperationRequests
            .FirstOrDefaultAsync(r => r.Id == input.OperationRequestId && r.TenantId == tid && !r.IsDeleted);

        if (request is null)
            return (false, "Yêu cầu vận hành không tồn tại.");

        if (request.Status is OperationStatus.Rejected or OperationStatus.Cancelled)
            return (false, "Không thể tạo công việc cho yêu cầu đã từ chối hoặc đã hủy.");

        var departmentId = input.OrganizationUnitId ?? request.OrganizationUnitId;
        var departmentExists = await db.OrganizationUnits
            .AnyAsync(o => o.Id == departmentId && o.TenantId == tid && o.IsActive && !o.IsDeleted);
        if (!departmentExists)
            return (false, "Phòng ban phụ trách không hợp lệ.");

        if (input.DueDate.HasValue && input.DueDate.Value < DateOnly.FromDateTime(DateTime.Today))
            return (false, "Hạn xử lý không được nhỏ hơn ngày hôm nay.");

        AppUser? assignee = null;
        if (input.AssignedToUserId.HasValue)
        {
            assignee = await db.AppUsers
                .FirstOrDefaultAsync(u => u.Id == input.AssignedToUserId.Value && u.TenantId == tid && u.Status == UserStatus.Active && !u.IsDeleted);
            if (assignee is null)
                return (false, "Người được giao không hợp lệ.");
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

        if (request.Status is OperationStatus.Approved or OperationStatus.Completed)
        {
            request.Status = OperationStatus.InProgress;
            request.UpdatedAt = DateTimeOffset.UtcNow;
            request.UpdatedByUserId = tenant.UserId;
        }

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid,
            UserId = tenant.UserId,
            UserName = tenant.UserFullName,
            Action = "Create",
            EntityName = "WorkItem",
            EntityId = workItem.Id,
            NewValuesJson = $"{{\"Title\":\"{workItem.Title}\",\"Status\":\"{workItem.Status}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return (true, "Đã tạo thẻ công việc trên Kanban.");
    }

    public async Task<(bool Success, string Message)> MoveAsync(Guid workItemId, WorkItemStatus newStatus)
    {
        var item = await db.WorkItems
            .Include(w => w.OperationRequest)
            .Include(w => w.Assignments)
            .FirstOrDefaultAsync(w => w.Id == workItemId && w.TenantId == tenant.TenantId && !w.IsDeleted);

        if (item is null)
            return (false, "Không tìm thấy công việc.");

        if (!Enum.IsDefined(newStatus))
            return (false, "Trạng thái Kanban không hợp lệ.");

        if (item.Status == newStatus)
            return (true, "Trạng thái công việc không thay đổi.");

        var oldStatus = item.Status;
        item.Status = newStatus;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = tenant.UserId;

        foreach (var assignment in item.Assignments.Where(a => !a.IsDeleted))
        {
            assignment.CompletedAt = newStatus == WorkItemStatus.Done ? DateTimeOffset.UtcNow : null;
            assignment.UpdatedAt = DateTimeOffset.UtcNow;
            assignment.UpdatedByUserId = tenant.UserId;
        }

        await SyncOperationStatusAsync(item);

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.TenantId,
            UserId = tenant.UserId,
            UserName = tenant.UserFullName,
            Action = "MoveKanbanCard",
            EntityName = "WorkItem",
            EntityId = item.Id,
            OldValuesJson = $"{{\"Status\":\"{oldStatus}\"}}",
            NewValuesJson = $"{{\"Status\":\"{newStatus}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return (true, "Đã cập nhật trạng thái Kanban.");
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

        request.Status = item.Status == WorkItemStatus.Done && remainingActiveItems == 0
            ? OperationStatus.Completed
            : OperationStatus.InProgress;
        request.UpdatedAt = DateTimeOffset.UtcNow;
        request.UpdatedByUserId = tenant.UserId;
    }

    private static List<KanbanColumnViewModel> GetKanbanColumns() =>
    [
        new() { Status = WorkItemStatus.Todo, Title = "Cần làm", Description = "Việc đã được ghi nhận và chờ xử lý.", AccentClass = "col-todo" },
        new() { Status = WorkItemStatus.InProgress, Title = "Đang xử lý", Description = "Việc đang được phòng ban phụ trách thực hiện.", AccentClass = "col-progress" },
        new() { Status = WorkItemStatus.Blocked, Title = "Đang vướng", Description = "Việc bị chặn bởi thiếu thông tin, nguồn lực hoặc phê duyệt.", AccentClass = "col-blocked" },
        new() { Status = WorkItemStatus.Done, Title = "Hoàn thành", Description = "Việc đã hoàn tất và sẵn sàng nghiệm thu/báo cáo.", AccentClass = "col-done" },
        new() { Status = WorkItemStatus.Cancelled, Title = "Đã hủy", Description = "Việc không tiếp tục thực hiện.", AccentClass = "col-cancelled" }
    ];

    private static string GetPriorityClass(PriorityLevel priority) => priority switch
    {
        PriorityLevel.Low => "priority-low",
        PriorityLevel.Normal => "priority-normal",
        PriorityLevel.High => "priority-high",
        PriorityLevel.Critical => "priority-critical",
        _ => "priority-normal"
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

    // ── Detail ─────────────────────────────────────────────────────────────────
    public async Task<WorkItemDetailViewModel?> GetDetailAsync(Guid id)
    {
        var tid = tenant.TenantId;
        var item = await db.WorkItems
            .Include(w => w.OperationRequest)
            .Include(w => w.OrganizationUnit)
            .Include(w => w.Assignments).ThenInclude(a => a.AssignedToUser)
            .Include(w => w.Checklists).ThenInclude(c => c.CompletedByUser)
            .Include(w => w.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tid && !w.IsDeleted);
        if (item == null) return null;

        var createdBy = item.CreatedByUserId.HasValue
            ? await db.AppUsers.Where(u => u.Id == item.CreatedByUserId.Value).Select(u => u.FullName).FirstOrDefaultAsync() ?? "—"
            : "Hệ thống";

        var activeAssignment = item.Assignments.Where(a => !a.IsDeleted).OrderByDescending(a => a.AssignedAt).FirstOrDefault();

        return new WorkItemDetailViewModel
        {
            Id = item.Id, Title = item.Title, Description = item.Description,
            Status = item.Status.ToString(),
            StatusLabel = GetStatusLabel(item.Status),
            Priority = item.Priority.ToString(), PriorityClass = GetPriorityClass(item.Priority),
            Department = item.OrganizationUnit?.Name ?? "", DepartmentId = item.OrganizationUnitId,
            RequestNo = item.OperationRequest?.RequestNo ?? "", RequestTitle = item.OperationRequest?.Title ?? "",
            OperationRequestId = item.OperationRequestId,
            AssignedTo = activeAssignment?.AssignedToUser?.FullName,
            AssignedToUserId = activeAssignment?.AssignedToUserId,
            DueDate = item.DueDate,
            IsOverdue = item.DueDate.HasValue && item.DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
                && item.Status != WorkItemStatus.Done && item.Status != WorkItemStatus.Cancelled,
            CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt, CreatedByName = createdBy,
            Checklists = item.Checklists.Where(c => !c.IsDeleted).OrderBy(c => c.SortOrder)
                .Select(c => new WorkItemChecklistItem { Id = c.Id, Title = c.Title, IsCompleted = c.IsCompleted, CompletedByName = c.CompletedByUser?.FullName, CompletedAt = c.CompletedAt }).ToList(),
            Comments = item.Comments.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt)
                .Select(c => new WorkItemCommentItem { Id = c.Id, Content = c.Content, UserName = c.User?.FullName ?? "", UserId = c.UserId, CreatedAt = c.CreatedAt }).ToList(),
            AssignmentHistory = item.Assignments.Where(a => !a.IsDeleted).OrderByDescending(a => a.AssignedAt)
                .Select(a => new WorkItemAssignmentItem { Id = a.Id, UserName = a.AssignedToUser?.FullName ?? "", AssignedAt = a.AssignedAt, CompletedAt = a.CompletedAt }).ToList()
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
            Departments = await GetDepartmentOptionsAsync(tid),
            Assignees = await GetAssigneeOptionsAsync(tid),
            StatusOptions = Enum.GetValues<WorkItemStatus>().Select(s => new SelectOption { Value = s.ToString(), Text = GetStatusLabel(s) }).ToList()
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
        item.Status = input.Status;
        if (input.OrganizationUnitId.HasValue) item.OrganizationUnitId = input.OrganizationUnitId.Value;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        item.UpdatedByUserId = tenant.UserId;

        // Handle assignment change
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

        if (oldStatus != input.Status)
        {
            foreach (var a in item.Assignments.Where(a => !a.IsDeleted))
            {
                a.CompletedAt = input.Status == WorkItemStatus.Done ? DateTimeOffset.UtcNow : null;
                a.UpdatedAt = DateTimeOffset.UtcNow;
            }
            await SyncOperationStatusAsync(item);
        }

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName,
            Action = "Update", EntityName = "WorkItem", EntityId = item.Id,
            NewValuesJson = $"{{\"Title\":\"{item.Title}\",\"Status\":\"{item.Status}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
        return (true, "Đã cập nhật công việc.");
    }

    // ── Delete ─────────────────────────────────────────────────────────────────
    public async Task<(bool Success, string Message)> DeleteAsync(Guid id)
    {
        var item = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenant.TenantId && !w.IsDeleted);
        if (item == null) return (false, "Không tìm thấy.");
        item.IsDeleted = true; item.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Delete", EntityName = "WorkItem", EntityId = id, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return (true, "Đã xóa công việc.");
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
    public async Task<(bool Success, string Message)> AddChecklistAsync(Guid workItemId, string title)
    {
        var item = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId && w.TenantId == tenant.TenantId && !w.IsDeleted);
        if (item == null) return (false, "Không tìm thấy.");
        var maxSort = await db.WorkItemChecklists.Where(c => c.WorkItemId == workItemId && !c.IsDeleted).MaxAsync(c => (int?)c.SortOrder) ?? 0;
        db.WorkItemChecklists.Add(new WorkItemChecklist
        {
            TenantId = tenant.TenantId, WorkItemId = workItemId,
            Title = title.Trim(), SortOrder = maxSort + 1,
            CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (true, "Đã thêm mục checklist.");
    }

    public async Task<(bool Success, string Message)> ToggleChecklistAsync(Guid checklistId)
    {
        var cl = await db.WorkItemChecklists.FirstOrDefaultAsync(c => c.Id == checklistId && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (cl == null) return (false, "Không tìm thấy.");
        cl.IsCompleted = !cl.IsCompleted;
        cl.CompletedByUserId = cl.IsCompleted ? tenant.UserId : null;
        cl.CompletedAt = cl.IsCompleted ? DateTimeOffset.UtcNow : null;
        cl.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return (true, cl.IsCompleted ? "Đã hoàn thành." : "Đã bỏ hoàn thành.");
    }

    public async Task<(bool Success, string Message)> DeleteChecklistAsync(Guid checklistId)
    {
        var cl = await db.WorkItemChecklists.FirstOrDefaultAsync(c => c.Id == checklistId && c.TenantId == tenant.TenantId && !c.IsDeleted);
        if (cl == null) return (false, "Không tìm thấy.");
        cl.IsDeleted = true; cl.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return (true, "Đã xóa mục checklist.");
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
}

// ─── Approval ────────────────────────────────────────────────────────────────
public class ApprovalService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<ApprovalTaskListViewModel> GetMyTasksAsync(string? search = null, string? statusFilter = null)
    {
        var tid = tenant.TenantId;
        var userRoles = tenant.Roles.ToList();
        var query = db.ApprovalTasks.Where(t => t.TenantId == tid && !t.IsDeleted &&
                (t.AssignedToUserId == tenant.UserId || (t.AssignedRole != null && userRoles.Contains(t.AssignedRole))));

        // Stats (before filtering)
        var pendingCount = await query.CountAsync(t => t.Status == ApprovalStatus.Pending);
        var approvedCount = await query.CountAsync(t => t.Status == ApprovalStatus.Approved);
        var rejectedCount = await query.CountAsync(t => t.Status == ApprovalStatus.Rejected);
        var totalCount = await query.CountAsync();

        var allTasks = await query.Include(t => t.AssignedToUser)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();

        var reqIds = allTasks.Where(t => t.TargetType != "SalesOrder").Select(t => t.TargetId).Distinct().ToList();
        var reqs = await db.OperationRequests
            .Include(r => r.OrganizationUnit)
            .Where(r => reqIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        var soIds = allTasks.Where(t => t.TargetType == "SalesOrder").Select(t => t.TargetId).Distinct().ToList();
        var salesOrders = await db.SalesOrders
            .Include(o => o.Customer)
            .Where(o => soIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id);

        // Creator lookup
        var creatorIds = reqs.Values.Where(r => r.CreatedByUserId.HasValue).Select(r => r.CreatedByUserId!.Value)
            .Concat(salesOrders.Values.Where(o => o.CreatedByUserId.HasValue).Select(o => o.CreatedByUserId!.Value))
            .Distinct().ToList();
        var creators = await db.AppUsers.Where(u => creatorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        ApprovalTaskItem Map(ApprovalTask t) {
            if (t.TargetType == "SalesOrder" && salesOrders.TryGetValue(t.TargetId, out var so))
            {
                var createdByName = so.CreatedByUserId.HasValue && creators.TryGetValue(so.CreatedByUserId.Value, out var n) ? n : null;
                return new ApprovalTaskItem
                {
                    Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode,
                    StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt đơn hàng" : "Ban giám đốc duyệt đơn hàng",
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

            reqs.TryGetValue(t.TargetId, out var req);
            var reqCreatedByName = req?.CreatedByUserId.HasValue == true && creators.TryGetValue(req.CreatedByUserId!.Value, out var rn) ? rn : null;
            return new ApprovalTaskItem
            {
                Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode,
                StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban lãnh đạo duyệt",
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

        if (t.TargetType == "SalesOrder")
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
                StepName = a.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban giám đốc duyệt",
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
            StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban giám đốc duyệt",
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
            WorkflowName = t.WorkflowInstance?.WorkflowDefinition?.Name ?? "Quy trình phê duyệt đơn hàng",
            WorkflowStatus = t.WorkflowInstance?.Status.ToString(),
            AllSteps = allSteps, AvailableAssignees = assignees
        };
    }

    public async Task<bool> ApproveAsync(Guid taskId, string? note)
    {
        var t = await db.ApprovalTasks.Include(a => a.WorkflowInstance).FirstOrDefaultAsync(a => a.Id == taskId);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != ApprovalStatus.Pending) return false;

        t.Status = ApprovalStatus.Approved;
        t.DecisionNote = note;
        t.DecidedAt = DateTimeOffset.UtcNow;
        t.UpdatedAt = DateTimeOffset.UtcNow;

        if (t.TargetType == "SalesOrder")
        {
            var so = await db.SalesOrders.FindAsync(t.TargetId);
            if (so != null)
            {
                if (t.StepCode == "DEPARTMENT_REVIEW")
                {
                    // Department Manager approved, now escalate to Executive
                    var nextTask = new ApprovalTask
                    {
                        TenantId = tenant.TenantId,
                        WorkflowInstanceId = t.WorkflowInstanceId,
                        TargetType = "SalesOrder",
                        TargetId = so.Id,
                        StepCode = "EXECUTIVE_REVIEW",
                        AssignedRole = "EXECUTIVE",
                        Status = ApprovalStatus.Pending
                    };
                    db.ApprovalTasks.Add(nextTask);
                    so.UpdatedAt = DateTimeOffset.UtcNow;
                }
                else if (t.StepCode == "EXECUTIVE_REVIEW")
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
        else
        {
            var req = await db.OperationRequests.FindAsync(t.TargetId);
            if (req != null) { req.Status = OperationStatus.Approved; req.UpdatedAt = DateTimeOffset.UtcNow; }
        }

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Approve", EntityName = "ApprovalTask", EntityId = taskId, NewValuesJson = "{\"Status\":\"Approved\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(Guid taskId, string reason)
    {
        var t = await db.ApprovalTasks.Include(a => a.WorkflowInstance).FirstOrDefaultAsync(a => a.Id == taskId);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != ApprovalStatus.Pending) return false;

        t.Status = ApprovalStatus.Rejected;
        t.DecisionNote = reason;
        t.DecidedAt = DateTimeOffset.UtcNow;
        t.UpdatedAt = DateTimeOffset.UtcNow;

        if (t.TargetType == "SalesOrder")
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
        else
        {
            var req = await db.OperationRequests.FindAsync(t.TargetId);
            if (req != null) { req.Status = OperationStatus.Rejected; req.UpdatedAt = DateTimeOffset.UtcNow; }
        }

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "Reject", EntityName = "ApprovalTask", EntityId = taskId, NewValuesJson = $"{{\"Status\":\"Rejected\",\"Reason\":\"{reason}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return true;
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
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName,
            Action = "Reassign", EntityName = "ApprovalTask", EntityId = taskId,
            OldValuesJson = $"{{\"AssignedToUserId\":\"{oldUserId}\"}}",
            NewValuesJson = $"{{\"AssignedToUserId\":\"{newUserId}\",\"AssignedToName\":\"{newUser.FullName}\"}}",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return (true, $"Đã chuyển giao cho {newUser.FullName}.");
    }

    public async Task<(bool Success, string Message)> ReturnForRevisionAsync(Guid taskId, string reason)
    {
        var t = await db.ApprovalTasks.Include(a => a.WorkflowInstance).FirstOrDefaultAsync(a => a.Id == taskId);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != ApprovalStatus.Pending)
            return (false, "Không thể trả lại.");

        t.Status = ApprovalStatus.Skipped;
        t.DecisionNote = $"Trả lại: {reason}";
        t.DecidedAt = DateTimeOffset.UtcNow;
        t.UpdatedAt = DateTimeOffset.UtcNow;

        if (t.TargetType == "SalesOrder")
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
        else
        {
            var req = await db.OperationRequests.FindAsync(t.TargetId);
            if (req != null) { req.Status = OperationStatus.Draft; req.UpdatedAt = DateTimeOffset.UtcNow; }
        }

        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "ReturnForRevision", EntityName = "ApprovalTask", EntityId = taskId, NewValuesJson = $"{{\"Status\":\"Skipped\",\"Reason\":\"{reason}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return (true, "Đã trả lại yêu cầu để chỉnh sửa.");
    }
}

// ─── AI Insight — Real Gemini Integration ────────────────────────────────────
public class AiInsightService(ApplicationDbContext db, ITenantContext tenant, GeminiService gemini)
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
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, UserName = tenant.UserFullName, Action = "AiQuery", EntityName = "AiInsight", EntityId = insight.Id, NewValuesJson = $"{{\"Model\":\"{result.ModelName ?? "local"}\",\"Tokens\":\"{result.InputTokens}+{result.OutputTokens}\",\"Risk\":\"{risk}\"}}", CreatedAt = DateTimeOffset.UtcNow });
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
            await db.OkrKeyResults.Where(k => k.TenantId == tid && !k.IsDeleted && k.TargetValue > 0).Select(k => (double)(k.CurrentValue / k.TargetValue * 100)).DefaultIfEmpty(0).AverageAsync(),
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
        var okrAvg = await db.OkrKeyResults.Where(k => k.TenantId == tid && !k.IsDeleted && k.TargetValue > 0).Select(k => (double)(k.CurrentValue / k.TargetValue * 100)).DefaultIfEmpty(0).AverageAsync();
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
