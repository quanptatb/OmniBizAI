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
        var q = db.OperationRequests.Where(r => r.TenantId == tid && !r.IsDeleted);
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
            Items = items, TotalCount = total, Page = page, SearchTerm = search, StatusFilter = status, PriorityFilter = priority, DeptFilter = deptId,
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
        var approvals = await db.ApprovalTasks.Where(t => t.TargetId == id && !t.IsDeleted)
            .Select(t => new ApprovalTaskItem { Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode, StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban lãnh đạo duyệt", Status = t.Status.ToString(), AssignedRole = t.AssignedRole, DecisionNote = t.DecisionNote, DecidedAt = t.DecidedAt }).ToListAsync();
        var workItems = await db.WorkItems.Where(w => w.OperationRequestId == id && !w.IsDeleted)
            .Select(w => new WorkItemListItem { Id = w.Id, Title = w.Title, Status = w.Status.ToString(), Priority = w.Priority.ToString(), DueDate = w.DueDate }).ToListAsync();
        var aiInsights = await db.AiInsights.Where(a => a.ContextId == id && a.TenantId == tenant.TenantId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt).Take(3)
            .Select(a => new AiInsightListItem { Id = a.Id, ContextType = a.ContextType, Question = a.Question, Summary = a.Summary, Recommendation = a.Recommendation, RiskLevel = a.RiskLevel.ToString(), Status = a.Status.ToString(), CreatedAt = a.CreatedAt }).ToListAsync();

        return new OperationRequestDetailViewModel
        {
            Id = r.Id, RequestNo = r.RequestNo, Title = r.Title, Type = r.Type, Status = r.Status.ToString(), Priority = r.Priority.ToString(),
            Department = dept?.Name ?? "", Customer = customer?.Name, CreatedBy = creator?.FullName ?? "",
            CreatedAt = r.CreatedAt, DueDate = r.DueDate, TotalAmount = r.TotalAmount, Description = r.Description,
            ApprovalTasks = approvals, WorkItems = workItems, AiInsights = aiInsights,
            CanEdit = r.Status is OperationStatus.Draft or OperationStatus.Rejected,
            CanSubmit = r.Status == OperationStatus.Draft,
            CanCancel = r.Status is OperationStatus.Draft or OperationStatus.Submitted
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
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, Action = "Create", EntityName = "OperationRequest", EntityId = entity.Id, NewValuesJson = $"{{\"RequestNo\":\"{entity.RequestNo}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> SubmitAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId || r.Status != OperationStatus.Draft) return false;
        r.Status = OperationStatus.Submitted; r.UpdatedAt = DateTimeOffset.UtcNow;
        db.ApprovalTasks.Add(new ApprovalTask { TenantId = tenant.TenantId, TargetType = "OperationRequest", TargetId = id, StepCode = "DEPARTMENT_REVIEW", AssignedRole = "DEPARTMENT_MANAGER", Status = ApprovalStatus.Pending, CreatedAt = DateTimeOffset.UtcNow });
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, Action = "Submit", EntityName = "OperationRequest", EntityId = id, OldValuesJson = "{\"Status\":\"Draft\"}", NewValuesJson = "{\"Status\":\"Submitted\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var r = await db.OperationRequests.FindAsync(id);
        if (r is null || r.TenantId != tenant.TenantId || r.Status is not (OperationStatus.Draft or OperationStatus.Submitted)) return false;
        r.Status = OperationStatus.Cancelled; r.UpdatedAt = DateTimeOffset.UtcNow;
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, Action = "Cancel", EntityName = "OperationRequest", EntityId = id, NewValuesJson = "{\"Status\":\"Cancelled\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<OperationRequestCreateViewModel> GetCreateFormAsync()
    {
        var tid = tenant.TenantId;
        return new OperationRequestCreateViewModel
        {
            Departments = await db.OrganizationUnits.Where(o => o.TenantId == tid && o.IsActive && !o.IsDeleted).Select(o => new SelectOption { Value = o.Id.ToString(), Text = o.Name }).ToListAsync(),
            Customers = await db.Customers.Where(c => c.TenantId == tid && c.IsActive && !c.IsDeleted).Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name }).ToListAsync()
        };
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
        new() { Status = WorkItemStatus.Todo, Title = "Cần làm", Description = "Việc đã được ghi nhận và chờ xử lý.", AccentClass = "todo" },
        new() { Status = WorkItemStatus.InProgress, Title = "Đang xử lý", Description = "Việc đang được phòng ban phụ trách thực hiện.", AccentClass = "progress" },
        new() { Status = WorkItemStatus.Blocked, Title = "Đang vướng", Description = "Việc bị chặn bởi thiếu thông tin, nguồn lực hoặc phê duyệt.", AccentClass = "blocked" },
        new() { Status = WorkItemStatus.Done, Title = "Hoàn thành", Description = "Việc đã hoàn tất và sẵn sàng nghiệm thu/báo cáo.", AccentClass = "done" },
        new() { Status = WorkItemStatus.Cancelled, Title = "Đã hủy", Description = "Việc không tiếp tục thực hiện.", AccentClass = "cancelled" }
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
}

// ─── Approval ────────────────────────────────────────────────────────────────
public class ApprovalService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<ApprovalTaskListViewModel> GetMyTasksAsync()
    {
        var tid = tenant.TenantId;
        var userRoles = tenant.Roles.ToList();
        var allTasks = await db.ApprovalTasks.Where(t => t.TenantId == tid && !t.IsDeleted &&
                (t.AssignedToUserId == tenant.UserId || (t.AssignedRole != null && userRoles.Contains(t.AssignedRole))))
            .OrderByDescending(t => t.CreatedAt).ToListAsync();

        var reqIds = allTasks.Select(t => t.TargetId).Distinct().ToList();
        var reqs = await db.OperationRequests.Where(r => reqIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id);

        ApprovalTaskItem Map(ApprovalTask t) { reqs.TryGetValue(t.TargetId, out var req);
            return new ApprovalTaskItem { Id = t.Id, TargetType = t.TargetType, TargetId = t.TargetId, StepCode = t.StepCode, StepName = t.StepCode == "DEPARTMENT_REVIEW" ? "Trưởng bộ phận duyệt" : "Ban lãnh đạo duyệt", Status = t.Status.ToString(), AssignedRole = t.AssignedRole, DecisionNote = t.DecisionNote, DecidedAt = t.DecidedAt, RequestTitle = req?.Title ?? "", RequestNo = req?.RequestNo ?? "", RequestPriority = req?.Priority.ToString() ?? "", RequestCreatedAt = req?.CreatedAt ?? DateTimeOffset.MinValue }; }

        return new ApprovalTaskListViewModel { PendingTasks = allTasks.Where(t => t.Status == ApprovalStatus.Pending).Select(Map).ToList(), CompletedTasks = allTasks.Where(t => t.Status != ApprovalStatus.Pending).Select(Map).ToList() };
    }

    public async Task<bool> ApproveAsync(Guid taskId, string? note)
    {
        var t = await db.ApprovalTasks.FindAsync(taskId);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != ApprovalStatus.Pending) return false;
        t.Status = ApprovalStatus.Approved; t.DecisionNote = note; t.DecidedAt = DateTimeOffset.UtcNow; t.UpdatedAt = DateTimeOffset.UtcNow;
        var req = await db.OperationRequests.FindAsync(t.TargetId);
        if (req != null) { req.Status = OperationStatus.Approved; req.UpdatedAt = DateTimeOffset.UtcNow; }
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, Action = "Approve", EntityName = "ApprovalTask", EntityId = taskId, NewValuesJson = "{\"Status\":\"Approved\"}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }

    public async Task<bool> RejectAsync(Guid taskId, string reason)
    {
        var t = await db.ApprovalTasks.FindAsync(taskId);
        if (t is null || t.TenantId != tenant.TenantId || t.Status != ApprovalStatus.Pending) return false;
        t.Status = ApprovalStatus.Rejected; t.DecisionNote = reason; t.DecidedAt = DateTimeOffset.UtcNow; t.UpdatedAt = DateTimeOffset.UtcNow;
        var req = await db.OperationRequests.FindAsync(t.TargetId);
        if (req != null) { req.Status = OperationStatus.Rejected; req.UpdatedAt = DateTimeOffset.UtcNow; }
        db.AuditLogs.Add(new AuditLog { TenantId = tenant.TenantId, UserId = tenant.UserId, Action = "Reject", EntityName = "ApprovalTask", EntityId = taskId, NewValuesJson = $"{{\"Status\":\"Rejected\",\"Reason\":\"{reason}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); return true;
    }
}

// ─── AI Insight ──────────────────────────────────────────────────────────────
public class AiInsightService(ApplicationDbContext db, ITenantContext tenant)
{
    public async Task<List<AiInsightListItem>> GetListAsync() =>
        await db.AiInsights.Where(a => a.TenantId == tenant.TenantId && !a.IsDeleted).OrderByDescending(a => a.CreatedAt)
            .Select(a => new AiInsightListItem { Id = a.Id, ContextType = a.ContextType, Question = a.Question, Summary = a.Summary, Recommendation = a.Recommendation, RiskLevel = a.RiskLevel.ToString(), Status = a.Status.ToString(), CreatedAt = a.CreatedAt })
            .ToListAsync();

    public async Task<AiInsightListItem> AnalyzeAsync(AiInsightCreateViewModel vm)
    {
        var tid = tenant.TenantId;
        var reqCount = await db.OperationRequests.CountAsync(r => r.TenantId == tid && !r.IsDeleted);
        var pendingCount = await db.ApprovalTasks.CountAsync(t => t.TenantId == tid && t.Status == ApprovalStatus.Pending && !t.IsDeleted);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var overdueCount = await db.OperationRequests.CountAsync(r => r.TenantId == tid && !r.IsDeleted && r.DueDate < today && r.Status != OperationStatus.Completed && r.Status != OperationStatus.Cancelled);

        var risk = overdueCount > 3 ? RiskLevel.High : overdueCount > 1 ? RiskLevel.Medium : RiskLevel.Low;
        var summary = $"Hệ thống hiện có {reqCount} yêu cầu vận hành, {pendingCount} chờ duyệt, {overdueCount} quá hạn. Phân tích dựa trên câu hỏi: \"{vm.Question[..Math.Min(80, vm.Question.Length)]}\".";
        var rec = overdueCount > 0 ? $"• Ưu tiên xử lý {overdueCount} yêu cầu quá hạn.\n• Xem xét phân bổ nguồn lực hợp lý.\n• Thiết lập cảnh báo tự động." : "• Vận hành đang ổn định. Tiếp tục duy trì chất lượng xử lý yêu cầu.";

        var insight = new AiInsight { TenantId = tid, ContextType = vm.ContextType, ContextId = vm.ContextId, Question = vm.Question, Summary = summary, Recommendation = rec, RiskLevel = risk, Status = AiInsightStatus.Draft, AskedByUserId = tenant.UserId, CreatedByUserId = tenant.UserId, CreatedAt = DateTimeOffset.UtcNow };
        db.AiInsights.Add(insight);
        db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = tenant.UserId, Action = "AiQuery", EntityName = "AiInsight", EntityId = insight.Id, NewValuesJson = $"{{\"RiskLevel\":\"{risk}\"}}", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        return new AiInsightListItem { Id = insight.Id, ContextType = insight.ContextType, Question = insight.Question, Summary = insight.Summary, Recommendation = insight.Recommendation, RiskLevel = insight.RiskLevel.ToString(), Status = insight.Status.ToString(), CreatedAt = insight.CreatedAt };
    }
}
