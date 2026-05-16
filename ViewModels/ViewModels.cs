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
    public Guid DepartmentId { get; set; }
    public string? Customer { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateOnly? DueDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Description { get; set; }
    public List<ApprovalTaskItem> ApprovalTasks { get; set; } = new();
    public List<WorkItemListItem> WorkItems { get; set; } = new();
    public List<AiInsightListItem> AiInsights { get; set; } = new();
    public List<ActivityLogItem> ActivityLog { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanCancel { get; set; }
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.Today) && Status != "Completed" && Status != "Cancelled";

    public string StatusLabel => Status switch
    {
        "Draft" => "Bản nháp",
        "Submitted" => "Đã gửi duyệt",
        "InReview" => "Đang xem xét",
        "Approved" => "Đã duyệt",
        "InProgress" => "Đang xử lý",
        "Completed" => "Hoàn thành",
        "Rejected" => "Bị từ chối",
        "Cancelled" => "Đã hủy",
        _ => Status
    };

    public string PriorityLabel => Priority switch
    {
        "Low" => "Thấp",
        "Normal" => "Bình thường",
        "High" => "Cao",
        "Critical" => "Nghiêm trọng",
        _ => Priority
    };
}

public class OperationRequestEditViewModel
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250)]
    public string Title { get; set; } = "";

    [Required]
    public string Type { get; set; } = "";

    [Required]
    public Guid OrganizationUnitId { get; set; }

    public Guid? CustomerId { get; set; }

    [Required]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public DateOnly? DueDate { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TotalAmount { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Customers { get; set; } = new();
}

public class ActivityLogItem
{
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public string ActionLabel => Action switch
    {
        "Create" => "Tạo yêu cầu",
        "Submit" => "Gửi phê duyệt",
        "Approve" => "Phê duyệt",
        "Reject" => "Từ chối",
        "Cancel" => "Hủy yêu cầu",
        "Update" => "Cập nhật",
        "MoveKanbanCard" => "Di chuyển Kanban",
        _ => Action
    };

    public string ActionIcon => Action switch
    {
        "Create" => "fa-plus-circle",
        "Submit" => "fa-paper-plane",
        "Approve" => "fa-check-circle",
        "Reject" => "fa-times-circle",
        "Cancel" => "fa-ban",
        "Update" => "fa-pen",
        "MoveKanbanCard" => "fa-arrows-up-down",
        _ => "fa-clock"
    };

    public string ActionColor => Action switch
    {
        "Create" => "var(--apple-blue)",
        "Submit" => "var(--info)",
        "Approve" => "var(--success)",
        "Reject" => "var(--danger)",
        "Cancel" => "var(--danger)",
        "Update" => "var(--warning)",
        _ => "var(--text-muted)"
    };
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
    /// <summary>Only subordinates the current user can assign to.</summary>
    public List<SelectOption> Assignees { get; set; } = new();
    public List<KanbanColumnViewModel> Columns { get; set; } = new();
    public WorkItemCreateViewModel CreateForm { get; set; } = new();
    public int TotalCards => Columns.Sum(c => c.Items.Count);
    public int OverdueCards => Columns.Sum(c => c.Items.Count(i => i.IsOverdue));
    public int BlockedCards => Columns.Where(c => c.Status == WorkItemStatus.Blocked).Sum(c => c.Items.Count);
    /// <summary>True if the user can manage (add/edit/delete) columns.</summary>
    public bool CanManageColumns { get; set; }
}

public class KanbanColumnViewModel
{
    /// <summary>Database column Id.</summary>
    public Guid Id { get; set; }
    /// <summary>Enum status this column represents (for hard-coded Kanban flow).</summary>
    public WorkItemStatus Status { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string AccentColor { get; set; } = "#94a3b8";
    /// <summary>CSS accent class name used in the Kanban UI.</summary>
    public string AccentClass { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsDoneColumn { get; set; }
    public bool IsCancelledColumn { get; set; }
    public List<KanbanCardViewModel> Items { get; set; } = new();
}

public class KanbanCardViewModel
{
    public Guid Id { get; set; }
    public Guid OperationRequestId { get; set; }
    public Guid KanbanColumnId { get; set; }
    /// <summary>Current WorkItemStatus of the card.</summary>
    public WorkItemStatus Status { get; set; }
    public string RequestNo { get; set; } = "";
    public string RequestTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Department { get; set; } = "";
    public string Priority { get; set; } = "";
    public string PriorityClass { get; set; } = "";
    public string? AssignedTo { get; set; }
    public DateOnly? DueDate { get; set; }
    public int ChecklistDone { get; set; }
    public int ChecklistTotal { get; set; }
    public bool IsOverdue => DueDate.HasValue
        && DueDate.Value < DateOnly.FromDateTime(DateTime.Today)
        && !IsDone && !IsCancelled;
    public bool IsDone { get; set; }
    public bool IsCancelled { get; set; }
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
    /// <summary>Target column Id (dynamic).</summary>
    public Guid ColumnId { get; set; }
    /// <summary>Target WorkItemStatus (for enum-based Kanban flow).</summary>
    public WorkItemStatus Status { get; set; }
    public string? Search { get; set; }
    public Guid? Dept { get; set; }
}

// ── Column management ────────────────────────────────────────────────────────
public class KanbanColumnCreateViewModel
{
    [Required(ErrorMessage = "Tiêu đề cột không được để trống")]
    [StringLength(100, ErrorMessage = "Tiêu đề không quá 100 ký tự")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string AccentColor { get; set; } = "#94a3b8";

    public bool IsDoneColumn { get; set; }
    public bool IsCancelledColumn { get; set; }
}

public class KanbanColumnEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề cột không được để trống")]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string AccentColor { get; set; } = "#94a3b8";

    public bool IsDoneColumn { get; set; }
    public bool IsCancelledColumn { get; set; }
}

public class KanbanColumnReorderViewModel
{
    public List<Guid> ColumnIds { get; set; } = new();
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

// ===== CRM — CUSTOMERS =====
public class CustomerListViewModel
{
    public List<CustomerListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? IndustryFilter { get; set; }
}

public class CustomerListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? TaxCode { get; set; }
    public string? Industry { get; set; }
    public bool IsActive { get; set; }
    public int ContactCount { get; set; }
    public int SiteCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CustomerCreateViewModel
{
    [Required(ErrorMessage = "Mã khách hàng không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên khách hàng không được để trống")]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TaxCode { get; set; }

    [StringLength(100)]
    public string? Industry { get; set; }
}

public class CustomerDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? TaxCode { get; set; }
    public string? Industry { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<CustomerContactItem> Contacts { get; set; } = new();
    public List<CustomerSiteItem> Sites { get; set; } = new();
    public int OperationRequestCount { get; set; }
}

public class CustomerContactItem
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? JobTitle { get; set; }
    public bool IsPrimary { get; set; }
}

public class CustomerSiteItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? City { get; set; }
}

// ===== CRM — VENDORS =====
public class VendorListViewModel
{
    public List<VendorListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
}

public class VendorListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? TaxCode { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int PurchaseOrderCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class VendorCreateViewModel
{
    [Required(ErrorMessage = "Mã nhà cung cấp không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TaxCode { get; set; }

    [StringLength(255)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }
}

// ===== CRM — PRODUCTS & SERVICES =====
public class ProductListViewModel
{
    public List<ProductListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? TypeFilter { get; set; }
    public Guid? CategoryFilter { get; set; }
    public List<SelectOption> Categories { get; set; } = new();
}

public class ProductListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? CategoryName { get; set; }
    public string? UnitName { get; set; }
    public decimal? StandardPrice { get; set; }
    public bool IsActive { get; set; }
}

public class ProductCreateViewModel
{
    [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loại không được để trống")]
    [StringLength(50)]
    public string Type { get; set; } = "Service";

    public Guid? ProductCategoryId { get; set; }
    public Guid? UnitOfMeasureId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ")]
    public decimal? StandardPrice { get; set; }

    public List<SelectOption> Categories { get; set; } = new();
    public List<SelectOption> Units { get; set; } = new();
}

// ===== PROCUREMENT =====
public class ProcurementListViewModel
{
    public List<ProcurementListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
}

public class ProcurementListItem
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Department { get; set; }
    public string RequestedBy { get; set; } = "";
    public DateOnly? NeededByDate { get; set; }
    public decimal TotalEstimated { get; set; }
    public int LineCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ProcurementCreateViewModel
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    public Guid? OrganizationUnitId { get; set; }
    public DateOnly? NeededByDate { get; set; }

    public List<ProcurementLineInput> Lines { get; set; } = new();
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Products { get; set; } = new();
}

public class ProcurementLineInput
{
    public Guid? ProductServiceId { get; set; }
    public string? ItemName { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal? EstimatedUnitPrice { get; set; }
}

public class ProcurementDetailViewModel
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Department { get; set; }
    public string RequestedBy { get; set; } = "";
    public DateOnly? NeededByDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ProcurementLineItem> Lines { get; set; } = new();
    public decimal TotalEstimated => Lines.Sum(l => l.Quantity * (l.EstimatedUnitPrice ?? 0));
    public bool CanSubmit { get; set; }
    public bool CanCancel { get; set; }
}

public class ProcurementLineItem
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? ItemName { get; set; }
    public decimal Quantity { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public decimal LineTotal => Quantity * (EstimatedUnitPrice ?? 0);
}

// ===== PURCHASE ORDERS =====
public class PurchaseOrderListViewModel
{
    public List<PurchaseOrderListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
}

public class PurchaseOrderListItem
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public int LineCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class PurchaseOrderCreateViewModel
{
    [Required(ErrorMessage = "Nhà cung cấp không được để trống")]
    public Guid VendorId { get; set; }

    public Guid? ProcurementRequestId { get; set; }
    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public List<PurchaseOrderLineInput> Lines { get; set; } = new();
    public List<SelectOption> Vendors { get; set; } = new();
    public List<SelectOption> ProcurementRequests { get; set; } = new();
    public List<SelectOption> Products { get; set; } = new();
}

public class PurchaseOrderLineInput
{
    public Guid? ProductServiceId { get; set; }
    public string? ItemName { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}

public class PurchaseOrderDetailViewModel
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string? ProcurementRequestNo { get; set; }
    public string Status { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PurchaseOrderLineItem> Lines { get; set; } = new();
}

public class PurchaseOrderLineItem
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? ItemName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

// ===== EXPENSES =====
public class ExpenseListViewModel
{
    public List<ExpenseListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? StatusFilter { get; set; }
    public Guid? BudgetFilter { get; set; }
    public List<SelectOption> Budgets { get; set; } = new();
}

public class ExpenseListItem
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string Status { get; set; } = "";
    public string? BudgetName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ExpenseCreateViewModel
{
    [Required(ErrorMessage = "Mô tả không được để trống")]
    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số tiền không được để trống")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? BudgetId { get; set; }

    public List<SelectOption> Budgets { get; set; } = new();
}

// ===== BUDGET CREATE/DETAILS =====
public class BudgetCreateViewModel
{
    [Required(ErrorMessage = "Tên ngân sách không được để trống")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? OrganizationUnitId { get; set; }

    [Required(ErrorMessage = "Năm tài chính không được để trống")]
    [Range(2020, 2050)]
    public int FiscalYear { get; set; } = DateTime.Today.Year;

    [Required(ErrorMessage = "Số tiền không được để trống")]
    [Range(0.01, double.MaxValue)]
    public decimal PlannedAmount { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
}

public class BudgetDetailViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public int FiscalYear { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal Remaining => PlannedAmount - UsedAmount;
    public decimal UsagePercent => PlannedAmount > 0 ? Math.Round(UsedAmount / PlannedAmount * 100, 1) : 0;
    public string Status { get; set; } = "";
    public List<ExpenseListItem> Expenses { get; set; } = new();
}

// ===== HR — EMPLOYEES =====
public class EmployeeListViewModel
{
    public List<EmployeeListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? SearchTerm { get; set; }
    public Guid? DeptFilter { get; set; }
    public List<SelectOption> Departments { get; set; } = new();
}

public class EmployeeListItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public string? CurrentContract { get; set; }
}

public class EmployeeDetailViewModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? StartDate { get; set; }
    public List<EmployeeContractItem> Contracts { get; set; } = new();
}

public class EmployeeContractItem
{
    public Guid Id { get; set; }
    public string ContractNo { get; set; } = "";
    public string ContractType { get; set; } = "";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsCurrent => EffectiveTo == null || EffectiveTo >= DateOnly.FromDateTime(DateTime.Today);
}

// ===== HR — POSITIONS =====
public class PositionListViewModel
{
    public List<PositionListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

public class PositionListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public bool IsManagerial { get; set; }
    public string? Department { get; set; }
}

public class PositionCreateViewModel
{
    [Required(ErrorMessage = "Mã chức vụ không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên chức vụ không được để trống")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; }
    public bool IsManagerial { get; set; }
    public Guid? OrganizationUnitId { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
}

// ===== SETTINGS =====
public class CompanySettingsViewModel
{
    public Guid TenantId { get; set; }

    [Required(ErrorMessage = "Tên công ty không được để trống")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TenantCode { get; set; }

    [StringLength(100)]
    public string? BusinessType { get; set; }

    // From BusinessProfile
    [StringLength(80)]
    public string? ProfileCode { get; set; }

    [StringLength(100)]
    public string? Industry { get; set; }
}

public class ModuleListViewModel
{
    public List<ModuleListItem> Items { get; set; } = new();
}

public class ModuleListItem
{
    public Guid Id { get; set; }
    public string ModuleCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusClass => Status switch { "Enabled" => "badge-on-track", "Trial" => "badge-at-risk", _ => "badge-late" };
    public string Icon => ModuleCode switch
    {
        "OPERATIONS" => "fa-list-check", "WORKFLOW" => "fa-table-columns", "APPROVALS" => "fa-circle-check",
        "FINANCE" => "fa-wallet", "PROCUREMENT" => "fa-cart-shopping", "CRM" => "fa-handshake",
        "HR" => "fa-users", "KPI_OKR" => "fa-bullseye", "REPORTS" => "fa-chart-bar",
        "AI_INSIGHTS" => "fa-robot", "AUDIT" => "fa-shield-halved", _ => "fa-puzzle-piece"
    };
    public DateOnly? EnabledFrom { get; set; }
    public DateOnly? EnabledTo { get; set; }
}

public class ParameterListViewModel
{
    public List<ParameterGroupItem> Groups { get; set; } = new();
}

public class ParameterGroupItem
{
    public string GroupName { get; set; } = "";
    public List<ParameterItem> Parameters { get; set; } = new();
}

public class ParameterItem
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public string ValueType { get; set; } = "String";
    public bool IsEditable { get; set; }
}

// ===== NOTIFICATIONS =====
public class NotificationItem
{
    public Guid DeliveryId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string SenderName { get; set; } = "Hệ thống";

    public string TimeAgo
    {
        get
        {
            var diff = DateTimeOffset.UtcNow - CreatedAt;
            if (diff.TotalMinutes < 1) return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
            return CreatedAt.ToLocalTime().ToString("dd/MM/yyyy");
        }
    }

    public string Icon => EntityName switch
    {
        "OperationRequest" => "fa-list-check",
        "ApprovalTask" => "fa-circle-check",
        "Budget" => "fa-piggy-bank",
        "Expense" => "fa-receipt",
        "ProcurementRequest" => "fa-cart-shopping",
        "PurchaseOrder" => "fa-file-invoice",
        "Customer" => "fa-building",
        "Vendor" => "fa-truck",
        "Employee" => "fa-id-badge",
        "KpiDefinition" => "fa-gauge-high",
        "OkrObjective" => "fa-bullseye",
        "PaymentRequest" => "fa-file-invoice-dollar",
        _ => "fa-bell"
    };
}

// ===== PROFILE =====
public class ProfileViewModel
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Locale { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTimeOffset? LastLogin { get; set; }
    public int TotalNotifications { get; set; }
    public int UnreadNotifications { get; set; }
}

public class ProfileEditViewModel
{
    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(200)]
    public string FullName { get; set; } = "";

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    public string? JobTitle { get; set; }

    [StringLength(100)]
    public string? TimeZoneId { get; set; }

    [StringLength(10)]
    public string? Locale { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Mật khẩu tối thiểu 3 ký tự")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = "";
}

