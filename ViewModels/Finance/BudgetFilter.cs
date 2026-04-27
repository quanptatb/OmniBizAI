using OmniBizAI.Models.Common;

namespace OmniBizAI.ViewModels.Finance;

/// <summary>
/// Bộ lọc danh sách ngân sách — kế thừa PagedRequest để có sẵn phân trang/search/sort.
/// Dùng trong BudgetsController.Index() để nhận query string filter từ UI.
/// </summary>
public sealed class BudgetFilter : PagedRequest
{
    /// <summary>Lọc theo trạng thái ngân sách (Active/Frozen/Closed)</summary>
    public string? Status { get; init; }

    /// <summary>Lọc theo phòng ban</summary>
    public Guid? DepartmentId { get; init; }

    /// <summary>Lọc theo danh mục ngân sách</summary>
    public Guid? CategoryId { get; init; }

    /// <summary>Lọc từ ngày (theo kỳ tài chính)</summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>Lọc đến ngày (theo kỳ tài chính)</summary>
    public DateOnly? DateTo { get; init; }
}
