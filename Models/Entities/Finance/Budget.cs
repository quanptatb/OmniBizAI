using OmniBizAI.Models.Entities.Organization;

namespace OmniBizAI.Models.Entities.Finance;

/// <summary>
/// Ngân sách — entity trung tâm của Finance module.
/// Bảng DB: budgets (snake_case).
/// Blueprint mục 14.4.3: chứa allocated/spent/committed/remaining amount, warning threshold.
/// 
/// Quy tắc nghiệp vụ:
/// - remaining_amount = allocated_amount - spent_amount - committed_amount
/// - utilization_pct = (spent_amount + committed_amount) / allocated_amount * 100
/// - utilization >= warning_threshold → cảnh báo vàng (Warning)
/// - utilization > 100 → cảnh báo đỏ (Critical)
/// </summary>
public class Budget : SoftDeletableEntity
{
    /// <summary>FK tới Company</summary>
    public Guid CompanyId { get; set; }

    /// <summary>FK tới kỳ tài chính</summary>
    public Guid FiscalPeriodId { get; set; }

    /// <summary>FK tới phòng ban sở hữu ngân sách</summary>
    public Guid DepartmentId { get; set; }

    /// <summary>FK tới danh mục ngân sách</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Tên hiển thị (ví dụ: "Marketing Ads - 05/2026")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tổng ngân sách được cấp</summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>Số tiền đã chi (từ transaction Completed)</summary>
    public decimal SpentAmount { get; set; }

    /// <summary>Số tiền cam kết (từ PR đang chờ duyệt/đã duyệt chưa chi)</summary>
    public decimal CommittedAmount { get; set; }

    /// <summary>Số tiền còn lại = Allocated - Spent - Committed</summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>Phần trăm sử dụng = (Spent + Committed) / Allocated * 100</summary>
    public decimal UtilizationPct { get; set; }

    /// <summary>Ngưỡng cảnh báo (%) — mặc định 80</summary>
    public decimal WarningThreshold { get; set; } = 80;

    /// <summary>Trạng thái: Active / Frozen / Closed</summary>
    public string Status { get; set; } = nameof(BudgetStatus.Active);

    /// <summary>Ghi chú nội bộ</summary>
    public string? Notes { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;
    public FiscalPeriod FiscalPeriod { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public BudgetCategory Category { get; set; } = null!;
}
