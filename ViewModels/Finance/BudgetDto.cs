namespace OmniBizAI.ViewModels.Finance;

/// <summary>
/// DTO hiển thị ngân sách trong danh sách và chi tiết.
/// Blueprint mục 3.8 naming convention: BudgetDto (dùng chung cho list/detail trong BAO-02).
/// Sau này nếu cần tách, sẽ có BudgetListItemDto và BudgetDetailDto riêng.
/// </summary>
public sealed class BudgetDto
{
    /// <summary>ID ngân sách</summary>
    public Guid Id { get; init; }

    /// <summary>Tên hiển thị (ví dụ: "Marketing Ads - 05/2026")</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>ID phòng ban</summary>
    public Guid DepartmentId { get; init; }

    /// <summary>Tên phòng ban hiển thị</summary>
    public string DepartmentName { get; init; } = string.Empty;

    /// <summary>ID danh mục</summary>
    public Guid CategoryId { get; init; }

    /// <summary>Tên danh mục hiển thị</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Tổng ngân sách được cấp</summary>
    public decimal AllocatedAmount { get; init; }

    /// <summary>Số tiền đã chi</summary>
    public decimal SpentAmount { get; init; }

    /// <summary>Số tiền cam kết (PR đang xử lý)</summary>
    public decimal CommittedAmount { get; init; }

    /// <summary>Số tiền còn lại</summary>
    public decimal RemainingAmount { get; init; }

    /// <summary>Phần trăm sử dụng ngân sách</summary>
    public decimal UtilizationPct { get; init; }

    /// <summary>Trạng thái: Active / Frozen / Closed</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Tên kỳ tài chính</summary>
    public string FiscalPeriodName { get; init; } = string.Empty;

    /// <summary>Ngưỡng cảnh báo (%)</summary>
    public decimal WarningThreshold { get; init; }
}
