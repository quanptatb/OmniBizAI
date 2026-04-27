namespace OmniBizAI.Models.Common;

/// <summary>
/// Base request cho các endpoint có phân trang.
/// Quy ước Blueprint mục 3.7: danh sách mặc định 20 dòng/trang, sort desc.
/// </summary>
public class PagedRequest
{
    /// <summary>Trang hiện tại (1-indexed)</summary>
    public int Page { get; init; } = 1;

    /// <summary>Số dòng mỗi trang (mặc định 20 theo Blueprint)</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Từ khóa tìm kiếm chung (search theo tên, mã, ...)</summary>
    public string? Search { get; init; }

    /// <summary>Tên field sắp xếp</summary>
    public string? SortBy { get; init; }

    /// <summary>Thứ tự sắp xếp: "asc" hoặc "desc"</summary>
    public string SortOrder { get; init; } = "desc";
}
