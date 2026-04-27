namespace OmniBizAI.ViewModels.Finance;

/// <summary>
/// DTO phân tích mức sử dụng ngân sách — dùng cho JSON endpoint UtilizationJson.
/// Chứa thông tin tính toán và mức cảnh báo để UI hiển thị badge màu.
/// 
/// Công thức:
/// - RemainingAmount = AllocatedAmount - SpentAmount - CommittedAmount
/// - UtilizationPct = (SpentAmount + CommittedAmount) / AllocatedAmount * 100
/// - WarningLevel: &gt;= 80% → "Warning" (vàng), &gt; 100% → "Critical" (đỏ), else "Normal"
/// </summary>
public sealed class BudgetUtilizationDto
{
    /// <summary>ID ngân sách</summary>
    public Guid BudgetId { get; init; }

    /// <summary>Tên ngân sách</summary>
    public string BudgetName { get; init; } = string.Empty;

    /// <summary>Tổng ngân sách được cấp</summary>
    public decimal AllocatedAmount { get; init; }

    /// <summary>Số tiền đã chi</summary>
    public decimal SpentAmount { get; init; }

    /// <summary>Số tiền cam kết</summary>
    public decimal CommittedAmount { get; init; }

    /// <summary>Số tiền còn lại</summary>
    public decimal RemainingAmount { get; init; }

    /// <summary>Phần trăm sử dụng (%)</summary>
    public decimal UtilizationPct { get; init; }

    /// <summary>
    /// Mức cảnh báo: "Normal" / "Warning" / "Critical".
    /// Dùng để UI hiển thị badge màu xanh/vàng/đỏ.
    /// </summary>
    public string WarningLevel { get; init; } = "Normal";
}
