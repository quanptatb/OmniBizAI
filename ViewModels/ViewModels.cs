using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.ViewModels;

// ===== COMMON =====
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class SelectOption
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
}

// ===== AUTH =====
public class LoginViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

// ===== DASHBOARD =====
public class DashboardViewModel
{
    public string UserFullName { get; set; } = "";
    public string UserRole { get; set; } = "";
    public string TenantName { get; set; } = "";

    // Stats
    public int TotalOperationRequests { get; set; }
    public int PendingApprovals { get; set; }
    public int OverdueTasks { get; set; }
    public int ActiveUsers { get; set; }

    // Charts data
    public List<StatusCountItem> RequestsByStatus { get; set; } = new();
    public List<DeptWorkloadItem> DeptWorkload { get; set; } = new();
    public List<MonthlyTrendItem> MonthlyTrend { get; set; } = new();

    // Recent
    public List<RecentRequestItem> RecentRequests { get; set; } = new();
    public List<RecentAuditItem> RecentAudits { get; set; } = new();
    public List<KpiSummaryItem> KpiSummaries { get; set; } = new();

    // Finance
    public decimal TotalBudget { get; set; }
    public decimal UsedBudget { get; set; }
    public decimal BudgetUsagePercent => TotalBudget > 0 ? Math.Round(UsedBudget / TotalBudget * 100, 1) : 0;
}

public class StatusCountItem { public string Status { get; set; } = ""; public int Count { get; set; } }
public class DeptWorkloadItem { public string Dept { get; set; } = ""; public int Count { get; set; } }
public class MonthlyTrendItem { public string Month { get; set; } = ""; public int Created { get; set; } public int Completed { get; set; } }
public class RecentRequestItem
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateOnly? DueDate { get; set; }
}
public class RecentAuditItem { public string Action { get; set; } = ""; public string UserName { get; set; } = ""; public string EntityType { get; set; } = ""; public DateTimeOffset OccurredAt { get; set; } }
public class KpiSummaryItem { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public decimal Target { get; set; } public decimal? Actual { get; set; } public string Unit { get; set; } = ""; public decimal Progress => Target > 0 && Actual.HasValue ? Math.Round(Actual.Value / Target * 100, 1) : 0; }

// ===== OPERATIONS =====
public class OperationRequestListViewModel
{
    public List<OperationRequestListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // Filters
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? PriorityFilter { get; set; }
    public Guid? DeptFilter { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
}

public class OperationRequestListItem
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Department { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.Today) && Status != "Completed" && Status != "Cancelled";
}

public class OperationRequestCreateViewModel
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250, ErrorMessage = "Tiêu đề không quá 250 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loại yêu cầu không được để trống")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phòng ban phụ trách không được để trống")]
    public Guid OrganizationUnitId { get; set; }

    public Guid? CustomerId { get; set; }

    [Required]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public DateOnly? DueDate { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Số tiền không hợp lệ")]
    public decimal? TotalAmount { get; set; }

    // Dropdowns
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Customers { get; set; } = new();
}

public class OperationRequestDetailViewModel
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Department { get; set; } = "";
    public string? Customer { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Description { get; set; }
    public List<ApprovalTaskItem> ApprovalTasks { get; set; } = new();
    public List<WorkItemListItem> WorkItems { get; set; } = new();
    public List<AiInsightListItem> AiInsights { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanCancel { get; set; }
}

// ===== APPROVALS =====
public class ApprovalTaskListViewModel
{
    public List<ApprovalTaskItem> PendingTasks { get; set; } = new();
    public List<ApprovalTaskItem> CompletedTasks { get; set; } = new();
}

public class ApprovalTaskItem
{
    public Guid Id { get; set; }
    public string TargetType { get; set; } = "";
    public Guid TargetId { get; set; }
    public string StepCode { get; set; } = "";
    public string StepName { get; set; } = "";
    public string Status { get; set; } = "";
    public string? AssignedRole { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    // From linked request
    public string RequestTitle { get; set; } = "";
    public string RequestNo { get; set; } = "";
    public string RequestPriority { get; set; } = "";
    public DateTimeOffset RequestCreatedAt { get; set; }
}

public class ApproveRejectViewModel
{
    public Guid TaskId { get; set; }
    public string Action { get; set; } = ""; // "Approve" or "Reject"

    [StringLength(1000)]
    public string? Note { get; set; }
}

// ===== ORGANIZATION =====
public class OrganizationUnitListViewModel
{
    public List<OrgUnitTreeItem> Tree { get; set; } = new();
}

public class OrgUnitTreeItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public List<OrgUnitTreeItem> Children { get; set; } = new();
}

public class OrgUnitCreateViewModel
{
    [Required(ErrorMessage = "Mã phòng ban không được để trống")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên phòng ban không được để trống")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }
    public Guid? ManagerUserId { get; set; }

    public List<SelectOption> ParentOptions { get; set; } = new();
    public List<SelectOption> UserOptions { get; set; } = new();
}

// ===== USERS =====
public class UserListViewModel
{
    public List<UserListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
}

public class UserListItem
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? JobTitle { get; set; }
    public string Department { get; set; } = "";
    public string Status { get; set; } = "";
    public string Roles { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public Guid? OrganizationUnitId { get; set; }

    [Required]
    public string Role { get; set; } = "STAFF";

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Mật khẩu không được để trống")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public List<SelectOption> DepartmentOptions { get; set; } = new();
    public List<SelectOption> RoleOptions { get; set; } = new();
}

// ===== FINANCE =====
public class BudgetListViewModel
{
    public List<BudgetListItem> Items { get; set; } = new();
}

public class BudgetListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal UsagePercent => TotalAmount > 0 ? Math.Round(UsedAmount / TotalAmount * 100, 1) : 0;
    public decimal Remaining => TotalAmount - UsedAmount;
    public string Status { get; set; } = "";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
}

public class PaymentRequestListViewModel
{
    public List<PaymentRequestListItem> Items { get; set; } = new();
}

public class PaymentRequestListItem
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public string Department { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

// ===== KPI =====
public class KpiListViewModel
{
    public List<KpiListItem> Items { get; set; } = new();
    public string? PeriodFilter { get; set; }
    public string? OwnerTypeFilter { get; set; }
}

public class KpiListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public string OwnerType { get; set; } = "";
    public string PeriodType { get; set; } = "";
    public decimal TargetValue { get; set; }
    public decimal? ActualValue { get; set; }
    public string? Department { get; set; }
    public decimal Progress => TargetValue > 0 && ActualValue.HasValue ? Math.Round(ActualValue.Value / TargetValue * 100, 1) : 0;
}

// ===== AI INSIGHTS =====
public class AiInsightCreateViewModel
{
    [Required(ErrorMessage = "Câu hỏi không được để trống")]
    [StringLength(1000, ErrorMessage = "Câu hỏi không quá 1000 ký tự")]
    public string Question { get; set; } = string.Empty;

    public string ContextType { get; set; } = "Dashboard";
    public Guid? ContextId { get; set; }
}

public class AiInsightListItem
{
    public Guid Id { get; set; }
    public string ContextType { get; set; } = "";
    public string Question { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? Recommendation { get; set; }
    public string RiskLevel { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ModelName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ===== AUDIT LOG =====
public class AuditLogListViewModel
{
    public List<AuditLogListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public string? EntityTypeFilter { get; set; }
    public string? ActionFilter { get; set; }
    public string? UserFilter { get; set; }
}

public class AuditLogListItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string Action { get; set; } = "";
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// ===== WORK ITEMS =====
public class WorkItemListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public string? AssignedTo { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class KanbanBoardViewModel
{
    public string? SearchTerm { get; set; }
    public Guid? DepartmentFilter { get; set; }
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> OperationRequests { get; set; } = new();
    public List<SelectOption> Assignees { get; set; } = new();
    public List<KanbanColumnViewModel> Columns { get; set; } = new();
    public WorkItemCreateViewModel CreateForm { get; set; } = new();
    public int TotalCards => Columns.Sum(c => c.Items.Count);
    public int BlockedCards => Columns.Where(c => c.Status == WorkItemStatus.Blocked).Sum(c => c.Items.Count);
    public int OverdueCards => Columns.Sum(c => c.Items.Count(i => i.IsOverdue));
}

public class KanbanColumnViewModel
{
    public WorkItemStatus Status { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string AccentClass { get; set; } = "";
    public List<KanbanCardViewModel> Items { get; set; } = new();
}

public class KanbanCardViewModel
{
    public Guid Id { get; set; }
    public Guid OperationRequestId { get; set; }
    public string RequestNo { get; set; } = "";
    public string RequestTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; }
    public string Department { get; set; } = "";
    public string Priority { get; set; } = "";
    public string PriorityClass { get; set; } = "";
    public string? AssignedTo { get; set; }
    public DateOnly? DueDate { get; set; }
    public int ChecklistDone { get; set; }
    public int ChecklistTotal { get; set; }
    public bool IsOverdue => DueDate.HasValue
        && DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
        && Status is not WorkItemStatus.Done and not WorkItemStatus.Cancelled;
}

public class WorkItemCreateViewModel
{
    [Required(ErrorMessage = "Yêu cầu vận hành không được để trống")]
    public Guid OperationRequestId { get; set; }

    [Required(ErrorMessage = "Tiêu đề công việc không được để trống")]
    [StringLength(250, ErrorMessage = "Tiêu đề không quá 250 ký tự")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Mô tả không quá 2000 ký tự")]
    public string? Description { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;
    public DateOnly? DueDate { get; set; }
}

public class WorkItemMoveViewModel
{
    public Guid Id { get; set; }
    public WorkItemStatus Status { get; set; }
    public string? Search { get; set; }
    public Guid? Dept { get; set; }
}

// ===== REPORTS =====
public class ReportFilterViewModel
{
    public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? OrganizationUnitId { get; set; }
    public string? Status { get; set; }
    public List<SelectOption> Departments { get; set; } = new();
}

public class ReportSummaryViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int PendingRequests { get; set; }
    public decimal CompletionRate => TotalRequests > 0 ? Math.Round((decimal)CompletedRequests / TotalRequests * 100, 1) : 0;
    public List<StatusCountItem> ByStatus { get; set; } = new();
    public List<DeptWorkloadItem> ByDepartment { get; set; } = new();
    public List<MonthlyTrendItem> MonthlyTrend { get; set; } = new();
}
