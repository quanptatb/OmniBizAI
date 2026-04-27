namespace OmniBizAI.Models.Entities.Finance;

/// <summary>
/// Kỳ tài chính (tháng hoặc quý) — dùng để nhóm ngân sách và giao dịch theo thời kỳ.
/// Bảng DB: fiscal_periods (snake_case).
/// Blueprint mục 5.2: Finance base gồm fiscal periods.
/// </summary>
public class FiscalPeriod : AuditableEntity
{
    /// <summary>Mã kỳ (ví dụ: "FP-2026-05", "FP-2026-Q2")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên hiển thị (ví dụ: "Tháng 05/2026")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Loại kỳ: Monthly / Quarterly / Yearly</summary>
    public string PeriodType { get; set; } = "Monthly";

    /// <summary>Ngày bắt đầu kỳ</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Ngày kết thúc kỳ</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>Trạng thái: Planning / Active / Closed</summary>
    public string Status { get; set; } = "Active";

    /// <summary>FK tới Company</summary>
    public Guid CompanyId { get; set; }

    // Navigation
    public virtual ICollection<Budget> Budgets { get; set; } = [];
}
