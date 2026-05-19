using System.ComponentModel.DataAnnotations;
using OmniBizAI.Helpers;
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

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email hoặc tên đăng nhập")]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Token { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Mật khẩu tối thiểu 3 ký tự")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = "";
}

public class AdminResetPasswordViewModel
{
    [Required] public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Mật khẩu tối thiểu 3 ký tự")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = "";
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
    public int DraftCount { get; set; }
    public int SubmittedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
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

    // Order lines
    public List<OrderLineInputViewModel> Lines { get; set; } = new();

    // Dropdowns
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Customers { get; set; } = new();
    public List<SelectOption> CustomerSites { get; set; } = new();
    public List<SelectOption> Products { get; set; } = new();
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
    public string? CustomerSiteName { get; set; }
    public List<OrderLineDisplayItem> Lines { get; set; } = new();
    public List<ApprovalTaskItem> ApprovalTasks { get; set; } = new();
    public List<WorkItemListItem> WorkItems { get; set; } = new();
    public List<AiInsightListItem> AiInsights { get; set; } = new();
    public List<ActivityLogItem> ActivityLog { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanCancel { get; set; }
    public bool CanStartWork { get; set; }
    public bool CanComplete { get; set; }
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

public class OrderLineInputViewModel
{
    public Guid? ProductServiceId { get; set; }
    [StringLength(250)]
    public string? ProductName { get; set; }
    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải > 0")]
    public decimal Quantity { get; set; } = 1;
    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }
    [StringLength(500)]
    public string? Note { get; set; }
    public decimal LineAmount => Quantity * (UnitPrice ?? 0);
}

public class OrderLineDisplayItem
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? LineAmount { get; set; }
    public string? Note { get; set; }
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
        "StartWork" => "Bắt đầu xử lý",
        "Complete" => "Hoàn thành",
        "Delete" => "Xóa",
        "ChangeStage" => "Chuyển giai đoạn",
        "ReturnForRevision" => "Trả lại chỉnh sửa",
        "Confirm" => "Xác nhận nhập kho",
        "MarkPaid" => "Đánh dấu đã thanh toán",
        "Close" => "Đóng",
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
        "StartWork" => "fa-play",
        "Complete" => "fa-check-double",
        "Delete" => "fa-trash",
        "ChangeStage" => "fa-diagram-next",
        "ReturnForRevision" => "fa-rotate-left",
        "Confirm" => "fa-clipboard-check",
        "MarkPaid" => "fa-money-check-dollar",
        "Close" => "fa-lock",
        "MoveKanbanCard" => "fa-arrows-up-down",
        _ => "fa-clock"
    };

    public string ActionColor => Action switch
    {
        "Create" => "var(--apple-blue)",
        "Submit" => "var(--info)",
        "Approve" or "Complete" or "Confirm" => "var(--success)",
        "Reject" or "Cancel" or "Delete" => "var(--danger)",
        "Update" or "ChangeStage" => "var(--warning)",
        "StartWork" => "#5856d6",
        "ReturnForRevision" => "#ff9500",
        "MarkPaid" => "#30b0c7",
        "Close" => "#636366",
        _ => "var(--text-muted)"
    };
}

// ===== APPROVALS =====
public class ApprovalTaskListViewModel
{
    public List<ApprovalTaskItem> PendingTasks { get; set; } = new();
    public List<ApprovalTaskItem> CompletedTasks { get; set; } = new();
    // Stats
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int TotalCount { get; set; }
    // Filters
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
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
    public string? AssignedToName { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    // From linked request
    public string RequestTitle { get; set; } = "";
    public string RequestNo { get; set; } = "";
    public string RequestPriority { get; set; } = "";
    public DateTimeOffset RequestCreatedAt { get; set; }
    public string? RequestDescription { get; set; }
    public string? RequestDepartment { get; set; }
    public string? RequestCreatedBy { get; set; }
}

public class ApproveRejectViewModel
{
    public Guid TaskId { get; set; }
    public string Action { get; set; } = ""; // "Approve" or "Reject"

    [StringLength(1000)]
    public string? Note { get; set; }
}

public class ApprovalTaskDetailViewModel
{
    public Guid Id { get; set; }
    public string TargetType { get; set; } = "";
    public Guid TargetId { get; set; }
    public string StepCode { get; set; } = "";
    public string StepName { get; set; } = "";
    public string Status { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public string? AssignedRole { get; set; }
    public string? AssignedToName { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    // Request
    public string RequestTitle { get; set; } = "";
    public string RequestNo { get; set; } = "";
    public string? RequestDescription { get; set; }
    public string RequestPriority { get; set; } = "";
    public string? RequestDepartment { get; set; }
    public string? RequestCreatedBy { get; set; }
    public DateTimeOffset RequestCreatedAt { get; set; }
    public string RequestStatus { get; set; } = "";
    // Workflow
    public string? WorkflowName { get; set; }
    public string? WorkflowStatus { get; set; }
    // All steps in this workflow
    public List<ApprovalStepItem> AllSteps { get; set; } = new();
    // Assignees for reassign
    public List<SelectOption> AvailableAssignees { get; set; } = new();
}

public class ApprovalStepItem
{
    public Guid Id { get; set; }
    public string StepCode { get; set; } = "";
    public string StepName { get; set; } = "";
    public string Status { get; set; } = "";
    public string? AssignedToName { get; set; }
    public string? AssignedRole { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsCurrent { get; set; }
}

// ===== ORGANIZATION =====
public class OrganizationUnitListViewModel
{
    public List<OrgUnitTreeItem> Tree { get; set; } = new();
    public int TotalActive { get; set; }
    public int TotalInactive { get; set; }
    public int TotalEmployees { get; set; }
}

public class OrgUnitTreeItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public Guid? ManagerUserId { get; set; }
    public string? ManagerName { get; set; }
    public string? ParentName { get; set; }
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

public class OrgUnitEditViewModel
{
    [Required] public Guid Id { get; set; }

    [Required(ErrorMessage = "Mã phòng ban không được để trống")]
    [StringLength(50)]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Tên phòng ban không được để trống")]
    [StringLength(200)]
    public string Name { get; set; } = "";

    public Guid? ParentId { get; set; }
    public Guid? ManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;

    public List<SelectOption> ParentOptions { get; set; } = new();
    public List<SelectOption> UserOptions { get; set; } = new();
}

public class OrgUnitDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public string? ParentName { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? ManagerUserId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public int ChildCount { get; set; }
    public int PositionCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public List<OrgDetailEmployee> Employees { get; set; } = new();
    public List<OrgDetailPosition> Positions { get; set; } = new();
    public List<OrgDetailChild> Children { get; set; } = new();
}

public class OrgDetailEmployee
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? JobTitle { get; set; }
    public string Status { get; set; } = "";
}

public class OrgDetailPosition
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public bool IsManagerial { get; set; }
}

public class OrgDetailChild
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public string? ManagerName { get; set; }
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
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public Guid? OrganizationUnitId { get; set; }

    [Required(ErrorMessage = "Vai trò không được để trống")]
    public string Role { get; set; } = "STAFF";

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Mật khẩu tối thiểu 3 ký tự")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool SendWelcomeEmail { get; set; } = true;

    public List<SelectOption> DepartmentOptions { get; set; } = new();
    public List<SelectOption> RoleOptions { get; set; } = new();
}

public class UserEditViewModel
{
    [Required] public Guid Id { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255)]
    public string Email { get; set; } = "";

    [StringLength(30)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public Guid? OrganizationUnitId { get; set; }

    [Required(ErrorMessage = "Vai trò không được để trống")]
    public string Role { get; set; } = "STAFF";

    public string Status { get; set; } = "Active";

    public List<SelectOption> DepartmentOptions { get; set; } = new();
    public List<SelectOption> RoleOptions { get; set; } = new();
    public List<SelectOption> StatusOptions { get; set; } = new();
}

public class UserDetailViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? JobTitle { get; set; }
    public string Department { get; set; } = "";
    public string Status { get; set; } = "";
    public string Roles { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Locale { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public int TotalNotifications { get; set; }
    public int AssignedTasks { get; set; }
    public bool IsLocked { get; set; }
}

// ===== FINANCE =====
// ===== FINANCE DASHBOARD =====
public class FinanceDashboardViewModel
{
    public decimal TotalBudget { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal BudgetRemaining => TotalBudget - TotalExpense;
    public decimal UsagePercent => TotalBudget > 0 ? Math.Round(TotalExpense / TotalBudget * 100, 1) : 0;
    public int ActiveBudgets { get; set; }
    public int PendingPayments { get; set; }
    public decimal PendingPaymentAmount { get; set; }
    public decimal ExpenseThisMonth { get; set; }
    public List<BudgetListItem> AlertBudgets { get; set; } = new(); // >70% usage
    public List<PaymentRequestListItem> RecentPayments { get; set; } = new();
    public List<ExpenseMonthItem> MonthlyExpenses { get; set; } = new();
}

public class ExpenseMonthItem
{
    public string Month { get; set; } = "";
    public decimal Amount { get; set; }
}

// ===== BUDGETS =====
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

    public string StatusLabel => Status switch
    {
        "Draft" => "Bản nháp", "Active" => "Đang hoạt động", "Closed" => "Đã đóng", "Cancelled" => "Đã hủy", _ => Status
    };

    public bool IsWarning => UsagePercent > 70 && Status == "Active";
    public bool IsDanger => UsagePercent > 90 && Status == "Active";
}

// ===== PAYMENT REQUESTS =====
public class PaymentRequestListViewModel
{
    public List<PaymentRequestListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? StatusFilter { get; set; }
}

public class PaymentRequestListItem
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public string Department { get; set; } = "";
    public string? VendorName { get; set; }
    public string? RequestedBy { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string StatusLabel => Status switch
    {
        "Draft" => "Bản nháp", "Submitted" => "Đã gửi duyệt", "Approved" => "Đã duyệt",
        "Paid" => "Đã thanh toán", "Rejected" => "Từ chối", "Cancelled" => "Đã hủy", _ => Status
    };
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.Today) && Status is not ("Paid" or "Cancelled" or "Rejected");
}

public class PaymentRequestCreateViewModel
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200)]
    public string Title { get; set; } = "";

    public Guid? VendorId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    [Required(ErrorMessage = "Số tiền không được để trống")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal TotalAmount { get; set; }

    public DateOnly? DueDate { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public List<SelectOption> Vendors { get; set; } = new();
    public List<SelectOption> PurchaseOrders { get; set; } = new();
}

public class PaymentRequestDetailViewModel
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public string? VendorName { get; set; }
    public string? PurchaseOrderNo { get; set; }
    public string RequestedBy { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ActivityLogItem> ActivityLog { get; set; } = new();

    public bool CanSubmit => Status == "Draft";
    public bool CanApprove => Status == "Submitted";
    public bool CanReject => Status == "Submitted";
    public bool CanMarkPaid => Status == "Approved";
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.Today) && Status is not ("Paid" or "Cancelled" or "Rejected");

    public string StatusLabel => Status switch
    {
        "Draft" => "Bản nháp", "Submitted" => "Đã gửi duyệt", "Approved" => "Đã duyệt",
        "Paid" => "Đã thanh toán", "Rejected" => "Từ chối", "Cancelled" => "Đã hủy", _ => Status
    };
}

public class BudgetEditViewModel
{
    public Guid Id { get; set; }
    public string CurrentName { get; set; } = "";

    [Required(ErrorMessage = "Tên ngân sách không được để trống")]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Số tiền kế hoạch không được để trống")]
    [Range(0.01, double.MaxValue)]
    public decimal PlannedAmount { get; set; }

    public decimal UsedAmount { get; set; }
    public string Department { get; set; } = "";
    public int FiscalYear { get; set; }
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
    public string? AskedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AiInsightDetailViewModel
{
    public Guid Id { get; set; }
    public string ContextType { get; set; } = "";
    public string Question { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? Recommendation { get; set; }
    public string RiskLevel { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ModelName { get; set; }
    public string? AskedByName { get; set; }
    public string? RawResponseJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class AiInsightIndexViewModel
{
    public List<AiInsightListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? ContextTypeFilter { get; set; }
    public string? RiskLevelFilter { get; set; }
    public string? SearchTerm { get; set; }
}

public class AiRecommendationItem
{
    public Guid Id { get; set; }
    public string Category { get; set; } = "";  // Operations, Finance, HR, Inventory, CashFlow, CRM, KPI
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Normal"; // Critical, High, Normal, Low
    public string Status { get; set; } = "New"; // New, Read, InProgress, Implemented, Dismissed
    public string? ActionUrl { get; set; }
    public string Icon { get; set; } = "fa-lightbulb";
    public DateTimeOffset CreatedAt { get; set; }
}

public class AiRecommendationsViewModel
{
    public List<AiRecommendationItem> Items { get; set; } = new();
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int NormalCount { get; set; }
    public int TotalNew { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}

// ===== ANOMALY ALERTS =====
public class AnomalyAlertItem
{
    public string Id { get; set; } = "";
    public string Module { get; set; } = "";         // Inventory, Finance, Operations, HR, CRM, CashFlow
    public string Severity { get; set; } = "Warning"; // Critical, Warning, Info
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? ActionUrl { get; set; }
    public string Icon { get; set; } = "fa-triangle-exclamation";
    public string? MetricValue { get; set; }
    public string? ThresholdValue { get; set; }
    public bool IsNew { get; set; } = true;
}

public class AnomalyDashboardViewModel
{
    public List<AnomalyAlertItem> Alerts { get; set; } = new();
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int TotalAlerts { get; set; }
    public DateTimeOffset ScanTime { get; set; }
    public string? ModuleFilter { get; set; }
    public string? SeverityFilter { get; set; }
    // Stock alerts from DB
    public List<StockAlertListItem> StockAlerts { get; set; } = new();
    public int ActiveStockAlerts { get; set; }
}

// ===== BACKUP & RESTORE =====
public class BackupDashboardViewModel
{
    public List<BackupFileItem> BackupFiles { get; set; } = new();
    public int TotalBackups { get; set; }
    public long TotalSize { get; set; }
    public string TotalSizeDisplay { get; set; } = "";
    public DateTime? LastBackupAt { get; set; }
    public string BackupDirectory { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public int SqlBackupCount { get; set; }
    public int JsonExportCount { get; set; }
    public DatabaseInfoViewModel? DatabaseInfo { get; set; }
}

public class BackupFileItem
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSize { get; set; }
    public string FileSizeDisplay { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string BackupType { get; set; } = "Full";
    public string DatabaseName { get; set; } = "";
}

public class BackupResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long FileSize { get; set; }
    public string? BackupType { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DatabaseInfoViewModel
{
    public string DatabaseName { get; set; } = "";
    public decimal SizeMB { get; set; }
    public string SizeDisplay { get; set; } = "";
    public int TableCount { get; set; }
    public DateTime? LastSqlBackupAt { get; set; }
    public string ServerVersion { get; set; } = "";
    public string? ErrorMessage { get; set; }
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

public class WorkItemEditViewModel
{
    [Required] public Guid Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250)]
    public string Title { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;
    public DateOnly? DueDate { get; set; }
    public WorkItemStatus Status { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Assignees { get; set; } = new();
    public List<SelectOption> StatusOptions { get; set; } = new();
}

public class WorkItemDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public string Priority { get; set; } = "";
    public string PriorityClass { get; set; } = "";
    public string Department { get; set; } = "";
    public Guid? DepartmentId { get; set; }
    public string RequestNo { get; set; } = "";
    public string RequestTitle { get; set; } = "";
    public Guid OperationRequestId { get; set; }
    public string? AssignedTo { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public List<WorkItemChecklistItem> Checklists { get; set; } = new();
    public List<WorkItemCommentItem> Comments { get; set; } = new();
    public List<WorkItemAssignmentItem> AssignmentHistory { get; set; } = new();
}

public class WorkItemChecklistItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
    public string? CompletedByName { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class WorkItemCommentItem
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public string UserName { get; set; } = "";
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class WorkItemAssignmentItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
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

public class FinanceReportViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public decimal TotalBudget { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal BudgetUtilization => TotalBudget > 0 ? Math.Round(TotalExpense / TotalBudget * 100, 1) : 0;
    public decimal TotalPayments { get; set; }
    public int PendingPaymentCount { get; set; }
    public decimal PendingPaymentAmount { get; set; }
    public int TotalBudgetCount { get; set; }
    public int ActiveBudgetCount { get; set; }
    public List<StatusCountItem> PaymentByStatus { get; set; } = new();
    public List<DeptWorkloadItem> ExpenseByDept { get; set; } = new();
    public List<MonthlyTrendItem> ExpenseTrend { get; set; } = new();
}

public class HrReportViewModel
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalPositions { get; set; }
    public int PendingLeaves { get; set; }
    public List<DeptWorkloadItem> ByDepartment { get; set; } = new();
    public List<StatusCountItem> ByStatus { get; set; } = new();
}

public class KpiOkrReportViewModel
{
    public int TotalOkr { get; set; }
    public int ActiveOkr { get; set; }
    public int CompletedOkr { get; set; }
    public decimal AvgOkrProgress { get; set; }
    public int TotalKpi { get; set; }
    public int ActiveKpi { get; set; }
    public int PendingCheckIns { get; set; }
    public int TotalEvaluations { get; set; }
    public List<StatusCountItem> OkrByStatus { get; set; } = new();
    public List<StatusCountItem> KpiByStatus { get; set; } = new();
}

public class ExecutiveReportViewModel
{
    public ReportSummaryViewModel Operations { get; set; } = new();
    public FinanceReportViewModel Finance { get; set; } = new();
    public HrReportViewModel Hr { get; set; } = new();
    public KpiOkrReportViewModel KpiOkr { get; set; } = new();
    public InventoryReportViewModel Inventory { get; set; } = new();
    public CashFlowReportViewModel CashFlow { get; set; } = new();
    public int TotalCustomers { get; set; }
    public int TotalVendors { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOpportunities { get; set; }
    public decimal OpportunityPipelineValue { get; set; }
}

public class InventoryReportViewModel
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int CriticalStockCount { get; set; }
    public int OverstockCount { get; set; }
    public int ActiveAlertCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public int TotalReceipts { get; set; }
    public int ConfirmedReceipts { get; set; }
    public decimal TotalReceivedQty { get; set; }
    public int TotalIssues { get; set; }
    public int ConfirmedIssues { get; set; }
    public decimal TotalIssuedQty { get; set; }
    public List<DeptWorkloadItem> TopProducts { get; set; } = new();
    public List<MonthlyTrendItem> ReceiptIssueTrend { get; set; } = new();
}

public class CashFlowReportViewModel
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => TotalIncome - TotalExpense;
    public int TotalTransactions { get; set; }
    public int PendingApproval { get; set; }
    public List<StatusCountItem> ByCategory { get; set; } = new();
    public List<StatusCountItem> ByPaymentMethod { get; set; } = new();
    public List<CashMonthSummary> MonthlyTrend { get; set; } = new();
}

public class CrmReportViewModel
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int TotalVendors { get; set; }
    public int TotalOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal WonValue { get; set; }
    public decimal WinRate => TotalOpportunities > 0 ? Math.Round((decimal)WonOpportunities / TotalOpportunities * 100, 1) : 0;
    public int TotalInteractions { get; set; }
    public List<StatusCountItem> OpportunityByStage { get; set; } = new();
    public List<DeptWorkloadItem> TopCustomersByRevenue { get; set; } = new();
    public List<MonthlyTrendItem> InteractionTrend { get; set; } = new();
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

// ===== CRM — CUSTOMER CARE (INTERACTIONS) =====
public class CustomerCareListViewModel
{
    public List<CrmInteractionItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PlannedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? TypeFilter { get; set; }
    public string? StatusFilter { get; set; }
    public Guid? CustomerFilter { get; set; }
    public List<SelectOption> Customers { get; set; } = new();
}

public class CrmInteractionItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? ContactName { get; set; }
    public string? AssignedToName { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Outcome { get; set; }
    public string? NextAction { get; set; }
    public DateTimeOffset? NextActionDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CustomerCareCreateViewModel
{
    public Guid? PreselectedCustomerId { get; set; }

    [Required(ErrorMessage = "Khách hàng không được để trống")]
    public Guid CustomerId { get; set; }

    public Guid? CustomerContactId { get; set; }

    [Required(ErrorMessage = "Loại tương tác không được để trống")]
    [StringLength(50)]
    public string Type { get; set; } = "Note";

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250)]
    public string Subject { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(30)]
    public string Priority { get; set; } = "Normal";

    public DateTimeOffset? ScheduledAt { get; set; }

    public int? DurationMinutes { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public List<SelectOption> Customers { get; set; } = new();
    public List<SelectOption> Contacts { get; set; } = new();
    public List<SelectOption> Users { get; set; } = new();
}

public class CustomerCareDetailViewModel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public string Subject { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public string Priority { get; set; } = "";
    public string PriorityLabel { get; set; } = "";
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? ContactName { get; set; }
    public string? AssignedToName { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Outcome { get; set; }
    public string? NextAction { get; set; }
    public DateTimeOffset? NextActionDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
}

public class CustomerCareEditViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? CustomerContactId { get; set; }

    [Required(ErrorMessage = "Loại tương tác không được để trống")]
    [StringLength(50)]
    public string Type { get; set; } = "Note";

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250)]
    public string Subject { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(30)]
    public string Priority { get; set; } = "Normal";

    public DateTimeOffset? ScheduledAt { get; set; }
    public int? DurationMinutes { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public List<SelectOption> Contacts { get; set; } = new();
    public List<SelectOption> Users { get; set; } = new();
    public string CustomerName { get; set; } = "";
}

public class CompleteInteractionViewModel
{
    public Guid Id { get; set; }
    [StringLength(2000)]
    public string? Outcome { get; set; }
    [StringLength(2000)]
    public string? NextAction { get; set; }
    public DateTimeOffset? NextActionDate { get; set; }
}

// ===== CRM — SALES OPPORTUNITIES =====
public class SalesOpportunityListViewModel
{
    public List<SalesOpportunityItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int LeadCount { get; set; }
    public int QualifiedCount { get; set; }
    public int ProposalCount { get; set; }
    public int NegotiationCount { get; set; }
    public int ClosedWonCount { get; set; }
    public int ClosedLostCount { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedValue { get; set; }
    public string? SearchTerm { get; set; }
    public string? StageFilter { get; set; }
    public string? TempFilter { get; set; }
    public Guid? CustomerFilter { get; set; }
    public List<SelectOption> Customers { get; set; } = new();
}

public class SalesOpportunityItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Stage { get; set; } = "";
    public decimal EstimatedValue { get; set; }
    public int Probability { get; set; }
    public string Temperature { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? AssignedToName { get; set; }
    public string? Source { get; set; }
    public DateOnly? ExpectedCloseDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class SalesOpportunityCreateViewModel
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250)]
    public string Title { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Khách hàng không được để trống")]
    public Guid CustomerId { get; set; }

    public Guid? CustomerContactId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị không hợp lệ")]
    public decimal EstimatedValue { get; set; }

    [Range(0, 100, ErrorMessage = "Xác suất phải từ 0-100")]
    public int Probability { get; set; } = 10;

    [StringLength(30)]
    public string Temperature { get; set; } = "Warm";

    [StringLength(250)]
    public string? Source { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public List<SelectOption> Customers { get; set; } = new();
    public List<SelectOption> Contacts { get; set; } = new();
    public List<SelectOption> Users { get; set; } = new();
}

public class SalesOpportunityDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Stage { get; set; } = "";
    public string StageLabel { get; set; } = "";
    public decimal EstimatedValue { get; set; }
    public int Probability { get; set; }
    public string Temperature { get; set; } = "";
    public string TemperatureLabel { get; set; } = "";
    public string? Source { get; set; }
    public DateOnly? ExpectedCloseDate { get; set; }
    public DateOnly? ActualCloseDate { get; set; }
    public string? LostReason { get; set; }
    public string? WonNote { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? ContactName { get; set; }
    public string? AssignedToName { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public decimal WeightedValue => EstimatedValue * Probability / 100m;
}

public class SalesOpportunityEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(250)]
    public string Title { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    public Guid CustomerId { get; set; }
    public Guid? CustomerContactId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EstimatedValue { get; set; }

    [Range(0, 100)]
    public int Probability { get; set; }

    [StringLength(30)]
    public string Temperature { get; set; } = "Warm";

    [StringLength(250)]
    public string? Source { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }
    public Guid? AssignedToUserId { get; set; }

    public string CustomerName { get; set; } = "";
    public List<SelectOption> Contacts { get; set; } = new();
    public List<SelectOption> Users { get; set; } = new();
}

public class ChangeStageViewModel
{
    public Guid Id { get; set; }
    public string NewStage { get; set; } = "";
    [StringLength(2000)]
    public string? Note { get; set; }
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

// ===== CRM — CUSTOMER EDIT =====
public class CustomerEditViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Mã KH không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = "";
    [Required(ErrorMessage = "Tên KH không được để trống")]
    [StringLength(250)]
    public string Name { get; set; } = "";
    [StringLength(100)]
    public string? TaxCode { get; set; }
    [StringLength(100)]
    public string? Industry { get; set; }
}

public class AddContactViewModel
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    [Required(ErrorMessage = "Tên liên hệ không được để trống")]
    [StringLength(200)]
    public string FullName { get; set; } = "";
    [StringLength(150)]
    public string? JobTitle { get; set; }
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255)]
    public string? Email { get; set; }
    [StringLength(30)]
    public string? PhoneNumber { get; set; }
    public bool IsPrimary { get; set; }
}

public class AddSiteViewModel
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    [Required(ErrorMessage = "Tên chi nhánh không được để trống")]
    [StringLength(200)]
    public string Name { get; set; } = "";
    [StringLength(500)]
    public string? Address { get; set; }
    [StringLength(100)]
    public string? City { get; set; }
}

// ===== CRM — VENDOR EDIT & DETAIL =====
public class VendorEditViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Mã NCC không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = "";
    [Required(ErrorMessage = "Tên NCC không được để trống")]
    [StringLength(250)]
    public string Name { get; set; } = "";
    [StringLength(100)]
    public string? TaxCode { get; set; }
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255)]
    public string? Email { get; set; }
    [StringLength(30)]
    public string? PhoneNumber { get; set; }
}

public class VendorDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? TaxCode { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int PurchaseOrderCount { get; set; }
    public int PaymentRequestCount { get; set; }
}

// ===== CRM — PRODUCT EDIT =====
public class ProductEditViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Mã SP không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = "";
    [Required(ErrorMessage = "Tên SP không được để trống")]
    [StringLength(250)]
    public string Name { get; set; } = "";
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = "Service";
    public Guid? ProductCategoryId { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? StandardPrice { get; set; }
    public List<SelectOption> Categories { get; set; } = new();
    public List<SelectOption> Units { get; set; } = new();
}

// ===== CRM — DASHBOARD =====
public class CrmDashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int TotalVendors { get; set; }
    public int ActiveVendors { get; set; }
    public int TotalProducts { get; set; }
    public int TotalContacts { get; set; }
    public List<CustomerListItem> RecentCustomers { get; set; } = new();
    public List<VendorListItem> RecentVendors { get; set; } = new();
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

// ===== GOODS RECEIPT (NHẬP KHO) =====
public class GoodsReceiptListViewModel
{
    public List<GoodsReceiptListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int ConfirmedCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
}

public class GoodsReceiptListItem
{
    public Guid Id { get; set; }
    public string ReceiptNo { get; set; } = "";
    public string PurchaseOrderNo { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly ReceiptDate { get; set; }
    public string? WarehouseLocation { get; set; }
    public string ReceivedBy { get; set; } = "";
    public int LineCount { get; set; }
    public decimal TotalReceivedQty { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class GoodsReceiptDetailViewModel
{
    public Guid Id { get; set; }
    public string ReceiptNo { get; set; } = "";
    public string PurchaseOrderNo { get; set; } = "";
    public Guid PurchaseOrderId { get; set; }
    public string VendorName { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly ReceiptDate { get; set; }
    public string? WarehouseLocation { get; set; }
    public string? Note { get; set; }
    public string ReceivedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public List<GoodsReceiptLineDisplay> Lines { get; set; } = new();
    public List<ActivityLogItem> ActivityLog { get; set; } = new();
    public bool CanConfirm { get; set; }
    public bool CanCancel { get; set; }
    public bool CanEdit { get; set; }

    public string StatusLabel => EnumHelper.Label<GoodsReceiptStatus>(Status);
}

public class GoodsReceiptLineDisplay
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public string? ItemName { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal? RejectedQuantity { get; set; }
    public bool QualityPassed { get; set; }
    public string? Note { get; set; }
    public decimal AcceptedQuantity => ReceivedQuantity - (RejectedQuantity ?? 0);
}

public class GoodsReceiptCreateViewModel
{
    [Required(ErrorMessage = "Đơn hàng mua không được để trống")]
    public Guid PurchaseOrderId { get; set; }

    public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(500)]
    public string? WarehouseLocation { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }

    public List<GoodsReceiptLineInput> Lines { get; set; } = new();

    // Dropdowns
    public List<SelectOption> PurchaseOrders { get; set; } = new();
    // PO details for auto-fill
    public string? SelectedPONo { get; set; }
    public string? SelectedVendor { get; set; }
}

public class GoodsReceiptLineInput
{
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? ProductServiceId { get; set; }
    [StringLength(250)]
    public string? ItemName { get; set; }
    public decimal OrderedQuantity { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Số lượng nhận phải >= 0")]
    public decimal ReceivedQuantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? RejectedQuantity { get; set; }
    public bool QualityPassed { get; set; } = true;
    [StringLength(500)]
    public string? Note { get; set; }
}

// ===== GOODS ISSUE (XUẤT KHO) =====
public class GoodsIssueListViewModel
{
    public List<GoodsIssueListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int ConfirmedCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
}

public class GoodsIssueListItem
{
    public Guid Id { get; set; }
    public string IssueNo { get; set; } = "";
    public string IssueType { get; set; } = "";
    public string? OperationRequestNo { get; set; }
    public string? CustomerName { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public string? WarehouseLocation { get; set; }
    public string IssuedBy { get; set; } = "";
    public int LineCount { get; set; }
    public decimal TotalIssuedQty { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string IssueTypeLabel => IssueType switch
    {
        "Internal" => "Nội bộ",
        "CustomerDelivery" => "Giao khách",
        "Transfer" => "Điều chuyển",
        "Adjustment" => "Điều chỉnh",
        _ => IssueType
    };

    public string IssueTypeIcon => IssueType switch
    {
        "Internal" => "fa-building",
        "CustomerDelivery" => "fa-truck-fast",
        "Transfer" => "fa-arrows-rotate",
        "Adjustment" => "fa-sliders",
        _ => "fa-box"
    };
}

public class GoodsIssueDetailViewModel
{
    public Guid Id { get; set; }
    public string IssueNo { get; set; } = "";
    public string IssueType { get; set; } = "";
    public string? OperationRequestNo { get; set; }
    public Guid? OperationRequestId { get; set; }
    public string? CustomerName { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "";
    public DateOnly IssueDate { get; set; }
    public string? WarehouseLocation { get; set; }
    public string? Destination { get; set; }
    public string? Note { get; set; }
    public string IssuedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public List<GoodsIssueLineDisplay> Lines { get; set; } = new();
    public List<ActivityLogItem> ActivityLog { get; set; } = new();
    public bool CanConfirm { get; set; }
    public bool CanCancel { get; set; }
    public bool CanEdit { get; set; }

    public string StatusLabel => EnumHelper.Label<GoodsIssueStatus>(Status);

    public string IssueTypeLabel => IssueType switch
    {
        "Internal" => "Nội bộ",
        "CustomerDelivery" => "Giao khách",
        "Transfer" => "Điều chuyển",
        "Adjustment" => "Điều chỉnh",
        _ => IssueType
    };
}

public class GoodsIssueLineDisplay
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public string? ItemName { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal IssuedQuantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? Note { get; set; }
    public decimal FulfillRate => RequestedQuantity > 0 ? IssuedQuantity / RequestedQuantity * 100 : 0;
}

public class GoodsIssueCreateViewModel
{
    [Required(ErrorMessage = "Loại xuất kho không được để trống")]
    [StringLength(50)]
    public string IssueType { get; set; } = "Internal";

    public Guid? OperationRequestId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? OrganizationUnitId { get; set; }

    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(500)]
    public string? WarehouseLocation { get; set; }

    [StringLength(500)]
    public string? Destination { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }

    public List<GoodsIssueLineInput> Lines { get; set; } = new();

    // Dropdowns
    public List<SelectOption> OperationRequests { get; set; } = new();
    public List<SelectOption> Customers { get; set; } = new();
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Products { get; set; } = new();
}

public class GoodsIssueLineInput
{
    public Guid? ProductServiceId { get; set; }
    [StringLength(250)]
    public string? ItemName { get; set; }
    [Range(0, double.MaxValue)]
    public decimal RequestedQuantity { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Số lượng xuất phải >= 0")]
    public decimal IssuedQuantity { get; set; }
    [StringLength(50)]
    public string? UnitOfMeasure { get; set; }
    [StringLength(500)]
    public string? Note { get; set; }
}

// ===== INVENTORY DASHBOARD & STOCK ALERTS =====
public class StockDashboardViewModel
{
    public List<StockItemViewModel> Items { get; set; } = new();
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int CriticalStockCount { get; set; }
    public int OverstockCount { get; set; }
    public int HealthyCount { get; set; }
    public int ActiveAlertCount { get; set; }
    public List<StockAlertListItem> RecentAlerts { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StockFilter { get; set; } // All, Low, Critical, Overstock, Healthy
    public string? CategoryFilter { get; set; }
    public List<SelectOption> Categories { get; set; } = new();
}

public class StockItemViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? CategoryName { get; set; }
    public string Type { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal MaxStock { get; set; }
    public decimal? StandardPrice { get; set; }
    public decimal StockValue => CurrentStock * (StandardPrice ?? 0);

    public string StockStatus
    {
        get
        {
            if (SafetyStock > 0 && CurrentStock <= SafetyStock) return "Critical";
            if (ReorderPoint > 0 && CurrentStock <= ReorderPoint) return "Low";
            if (MaxStock > 0 && CurrentStock >= MaxStock) return "Overstock";
            return "Healthy";
        }
    }

    public string StockStatusLabel => StockStatus switch
    {
        "Critical" => "Nguy hiểm",
        "Low" => "Thấp",
        "Overstock" => "Vượt mức",
        "Healthy" => "Bình thường",
        _ => StockStatus
    };

    public string StockStatusColor => StockStatus switch
    {
        "Critical" => "var(--danger)",
        "Low" => "var(--warning)",
        "Overstock" => "#5856d6",
        "Healthy" => "var(--success)",
        _ => "var(--text-muted)"
    };

    public string StockStatusBadge => StockStatus switch
    {
        "Critical" => "badge-behind",
        "Low" => "badge-at-risk",
        "Overstock" => "badge-pending",
        "Healthy" => "badge-on-track",
        _ => ""
    };

    public decimal StockPercent => ReorderPoint > 0 ? Math.Min(200, CurrentStock / ReorderPoint * 100) : (CurrentStock > 0 ? 100 : 0);
}

public class StockAlertListViewModel
{
    public List<StockAlertListItem> Items { get; set; } = new();
    public int ActiveCount { get; set; }
    public int AcknowledgedCount { get; set; }
    public int ResolvedCount { get; set; }
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
}

public class StockAlertListItem
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string AlertType { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal Threshold { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }

    public string AlertTypeLabel => AlertType switch
    {
        "Critical" => "Nguy hiểm",
        "Low" => "Tồn kho thấp",
        "Overstock" => "Vượt mức tồn",
        _ => AlertType
    };

    public string AlertTypeIcon => AlertType switch
    {
        "Critical" => "fa-triangle-exclamation",
        "Low" => "fa-arrow-down",
        "Overstock" => "fa-arrow-up",
        _ => "fa-bell"
    };

    public string AlertTypeColor => AlertType switch
    {
        "Critical" => "var(--danger)",
        "Low" => "var(--warning)",
        "Overstock" => "#5856d6",
        _ => "var(--text-muted)"
    };
}

public class SetStockThresholdsViewModel
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal CurrentStock { get; set; }
    [Range(0, double.MaxValue)]
    public decimal ReorderPoint { get; set; }
    [Range(0, double.MaxValue)]
    public decimal SafetyStock { get; set; }
    [Range(0, double.MaxValue)]
    public decimal MaxStock { get; set; }
}

// ===== CASH BOOK (THU CHI) =====
public class CashBookDashboardViewModel
{
    public List<CashTransactionListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => TotalIncome - TotalExpense;
    public int RecordedCount { get; set; }
    public int ApprovedCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? TypeFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? CategoryFilter { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public List<SelectOption> Categories { get; set; } = new();
    public List<CashMonthSummary> MonthlySummary { get; set; } = new();
}

public class CashTransactionListItem
{
    public Guid Id { get; set; }
    public string TransactionNo { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNo { get; set; }
    public string? CustomerName { get; set; }
    public string? VendorName { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "";
    public string RecordedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsIncome => TransactionType == "Income";
}

public class CashTransactionDetailViewModel
{
    public Guid Id { get; set; }
    public string TransactionNo { get; set; } = "";
    public string TransactionType { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReferenceNo { get; set; }
    public string? CustomerName { get; set; }
    public string? VendorName { get; set; }
    public string? BudgetName { get; set; }
    public string? Department { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "";
    public string RecordedBy { get; set; } = "";
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ActivityLogItem> ActivityLog { get; set; } = new();
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanVoid { get; set; }
    public bool IsIncome => TransactionType == "Income";
    public string StatusLabel => EnumHelper.Label<CashTransactionStatus>(Status);
}

public class CashTransactionCreateViewModel
{
    [Required(ErrorMessage = "Loại giao dịch không được để trống")]
    [StringLength(20)]
    public string TransactionType { get; set; } = "Expense";

    [Required(ErrorMessage = "Danh mục không được để trống")]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả không được để trống")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal Amount { get; set; }

    public DateOnly TransactionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    [StringLength(250)]
    public string? ReferenceNo { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? BudgetId { get; set; }
    public Guid? OrganizationUnitId { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }

    // Dropdowns
    public List<SelectOption> Customers { get; set; } = new();
    public List<SelectOption> Vendors { get; set; } = new();
    public List<SelectOption> Budgets { get; set; } = new();
    public List<SelectOption> Departments { get; set; } = new();
}

public class CashMonthSummary
{
    public string Month { get; set; } = "";
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance => Income - Expense;
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

// ===== HR — LEAVE MANAGEMENT =====
public class LeaveListViewModel
{
    public List<LeaveListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string? StatusFilter { get; set; }
    public List<SelectOption> Employees { get; set; } = new();
}

public class LeaveListItem
{
    public Guid Id { get; set; }
    public string EmployeeName { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string? Department { get; set; }
    public string LeaveType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalDays { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string LeaveTypeLabel => LeaveType switch
    {
        "Annual" => "Phép năm", "Sick" => "Ốm đau", "Personal" => "Việc riêng",
        "Maternity" => "Thai sản", "Unpaid" => "Không lương", _ => LeaveType
    };
    public string StatusLabel => Status switch
    {
        "Draft" => "Bản nháp", "Submitted" => "Chờ duyệt", "Approved" => "Đã duyệt",
        "Rejected" => "Từ chối", "Cancelled" => "Đã hủy", _ => Status
    };
}

public class LeaveCreateViewModel
{
    [Required(ErrorMessage = "Loại nghỉ phép không được để trống")]
    public string LeaveType { get; set; } = "Annual";

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [StringLength(500)]
    public string? Reason { get; set; }
}

// ===== HR — EMPLOYEE CREATE/EDIT =====
public class EmployeeCreateViewModel
{
    [Required(ErrorMessage = "Mã nhân viên không được để trống")]
    [StringLength(50)]
    public string EmployeeCode { get; set; } = "";

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255)]
    public string Email { get; set; } = "";

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? OrganizationUnitId { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
}

public class EmployeeEditViewModel
{
    public Guid ProfileId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeCode { get; set; } = "";

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(200)]
    public string FullName { get; set; } = "";

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public DateOnly? StartDate { get; set; }
    public Guid? OrganizationUnitId { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
}

public class AddContractViewModel
{
    public Guid EmployeeProfileId { get; set; }
    public string EmployeeName { get; set; } = "";

    [Required(ErrorMessage = "Số hợp đồng không được để trống")]
    [StringLength(80)]
    public string ContractNo { get; set; } = "";

    [Required(ErrorMessage = "Loại hợp đồng không được để trống")]
    [StringLength(80)]
    public string ContractType { get; set; } = "";

    [Required(ErrorMessage = "Ngày hiệu lực không được để trống")]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? EffectiveTo { get; set; }
}

// ===== HR — POSITION EDIT =====
public class PositionEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Mã chức vụ không được để trống")]
    [StringLength(80)]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Tên chức vụ không được để trống")]
    [StringLength(150)]
    public string Name { get; set; } = "";

    public int Level { get; set; }
    public bool IsManagerial { get; set; }
    public Guid? OrganizationUnitId { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
}

// ===== HR — DASHBOARD =====
public class HrDashboardViewModel
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int InactiveEmployees { get; set; }
    public int NewThisMonth { get; set; }
    public int ExpiringContracts { get; set; }
    public int OnLeaveToday { get; set; }
    public int PendingLeaves { get; set; }
    public List<DeptDistributionItem> DeptDistribution { get; set; } = new();
    public List<EmployeeListItem> RecentEmployees { get; set; } = new();
    public List<ExpiringContractItem> ExpiringContractList { get; set; } = new();
}

public class DeptDistributionItem
{
    public string DepartmentName { get; set; } = "";
    public int Count { get; set; }
    public decimal Percent { get; set; }
}

public class ExpiringContractItem
{
    public Guid ProfileId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string ContractNo { get; set; } = "";
    public DateOnly ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
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

// ===== THEME / APPEARANCE SETTINGS =====
public class ThemeSettingsViewModel
{
    // Brand Colors
    public string AccentColor { get; set; } = "#0066cc";
    public string AccentHover { get; set; } = "#0071e3";
    public string AccentDark { get; set; } = "#2997ff";
    // Surfaces
    public string CanvasColor { get; set; } = "#ffffff";
    public string ParchmentColor { get; set; } = "#f5f5f7";
    public string InkColor { get; set; } = "#1d1d1f";
    // Sidebar
    public string SidebarBg { get; set; } = "#1d1d1f";
    public string SidebarText { get; set; } = "#ffffff";
    // Semantic
    public string SuccessColor { get; set; } = "#34c759";
    public string WarningColor { get; set; } = "#ff9f0a";
    public string DangerColor { get; set; } = "#ff3b30";
    public string InfoColor { get; set; } = "#5ac8fa";
    // Typography
    public string FontFamily { get; set; } = "Inter";
    public string FontSize { get; set; } = "17";
    // Layout
    public string BorderRadius { get; set; } = "12";
    public string SidebarWidth { get; set; } = "260";
    // Branding
    public string LogoIcon { get; set; } = "fa-solid fa-brain";
    public string BrandName { get; set; } = "OmniBizAI";
    public string BrandTagline { get; set; } = "Smart Operations";
    // Dark mode
    public bool DarkMode { get; set; }
}

public class EnumLabelManagementViewModel
{
    public List<EnumLabelGroupVm> Groups { get; set; } = new();
}

public class EnumLabelGroupVm
{
    public string EnumTypeName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<EnumLabelItemVm> Items { get; set; } = new();
}

public class EnumLabelItemVm
{
    public string FullKey { get; set; } = "";
    public string EnumValue { get; set; } = "";
    public string DefaultLabel { get; set; } = "";
    public string? CustomLabel { get; set; }
    public Guid? ParameterId { get; set; }
    public bool IsOverridden { get; set; }
    /// <summary>Effective label: custom if set, else default</summary>
    public string EffectiveLabel => CustomLabel ?? DefaultLabel;
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


public class ResourceManagementDashboardViewModel
{
    public HumanResourceOverviewViewModel Human { get; set; } = new();
    public EquipmentResourceOverviewViewModel Equipment { get; set; } = new();
    public InventoryResourceOverviewViewModel Inventory { get; set; } = new();
    public InfrastructureResourceOverviewViewModel Infrastructure { get; set; } = new();
}

public class HumanResourceOverviewViewModel
{
    public int TotalEmployees { get; set; }
    public int OnLeaveToday { get; set; }
    public int PendingLeaves { get; set; }
    public decimal AvgPerformanceScore { get; set; }
}

public class EquipmentResourceOverviewViewModel
{
    public int TotalMaintenanceRequests { get; set; }
    public int ActiveMaintenanceRequests { get; set; }
    public int CompletedMaintenanceRequests { get; set; }
    public int OverdueMaintenanceRequests { get; set; }
}

public class InventoryResourceOverviewViewModel
{
    public int TotalProducts { get; set; }
    public int ActiveStockAlerts { get; set; }
    public int GoodsReceiptCount { get; set; }
    public int GoodsIssueCount { get; set; }
}

public class InfrastructureResourceOverviewViewModel
{
    public int TotalDepartments { get; set; }
    public int TotalPositions { get; set; }
    public int TotalWorkCalendars { get; set; }
    public int TotalCustomerSites { get; set; }
}

public class MaintenanceDashboardViewModel
{
    public string? Mode { get; set; }
    public int TotalRequests { get; set; }
    public int PreventiveCount { get; set; }
    public int CorrectiveCount { get; set; }
    public int OverdueCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalSparePartLines { get; set; }
    public decimal TotalSparePartQuantity { get; set; }
    public int IotSignalCount { get; set; }
    public List<MaintenanceRequestItemViewModel> Requests { get; set; } = new();
}

public class MaintenanceRequestItemViewModel
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public decimal? TotalAmount { get; set; }
    public int SparePartLines { get; set; }
    public decimal SparePartQuantity { get; set; }
}

public class OrderProcessDashboardViewModel
{
    public string? StatusFilter { get; set; }
    public int TotalOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int PendingApprovals { get; set; }
    public int QaCheckedOrders { get; set; }
    public List<OrderProcessItemViewModel> Items { get; set; } = new();
}

public class OrderProcessItemViewModel
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = "";
    public string Title { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int ApprovalLevels { get; set; }
    public int ApprovedLevels { get; set; }
    public int PendingApprovalLevels { get; set; }
    public decimal? QcPassRate { get; set; }
    public string TraceabilityCode { get; set; } = "";
}
