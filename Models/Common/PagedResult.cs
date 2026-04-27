namespace OmniBizAI.Models.Common;

/// <summary>
/// Kết quả phân trang generic — dùng cho mọi service trả danh sách.
/// Blueprint mục 3.8: Query list trả PagedResult&lt;T&gt;.
/// </summary>
public sealed class PagedResult<T>
{
    /// <summary>Danh sách items của trang hiện tại</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Tổng số bản ghi (trước phân trang)</summary>
    public int TotalCount { get; init; }

    /// <summary>Trang hiện tại</summary>
    public int Page { get; init; }

    /// <summary>Số dòng mỗi trang</summary>
    public int PageSize { get; init; }

    /// <summary>Tổng số trang</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Có trang trước không</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Có trang sau không</summary>
    public bool HasNextPage => Page < TotalPages;
}
