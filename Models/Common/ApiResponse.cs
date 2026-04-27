namespace OmniBizAI.Models.Common;

/// <summary>
/// Response wrapper chuẩn cho JSON endpoint phụ trợ (AJAX/chart/notification/AI).
/// Blueprint mục 3.7: ApiResponse chỉ dùng cho JSON phụ trợ, không phải MVC page action.
/// </summary>
public sealed class ApiResponse<T>
{
    /// <summary>Trạng thái thành công/thất bại</summary>
    public bool Success { get; init; }

    /// <summary>Dữ liệu trả về (null nếu lỗi)</summary>
    public T? Data { get; init; }

    /// <summary>Thông báo chính</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Danh sách lỗi chi tiết</summary>
    public IReadOnlyList<ApiError> Errors { get; init; } = [];

    /// <summary>Thông tin phân trang (nếu có)</summary>
    public PaginationMeta? Pagination { get; init; }

    /// <summary>Mã trace để debug</summary>
    public string? TraceId { get; init; }

    // --- Factory methods để tạo response nhanh ---

    /// <summary>Tạo response thành công</summary>
    public static ApiResponse<T> Ok(T data, string message = "Thành công") => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    /// <summary>Tạo response thất bại</summary>
    public static ApiResponse<T> Fail(string message, string? traceId = null) => new()
    {
        Success = false,
        Message = message,
        TraceId = traceId
    };
}

/// <summary>
/// Chi tiết một lỗi trong ApiResponse.
/// </summary>
public sealed class ApiError
{
    /// <summary>Mã lỗi (ví dụ: BUDGET_NOT_FOUND)</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Thông báo lỗi tiếng Việt</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Field liên quan (nếu là validation error)</summary>
    public string? Field { get; init; }
}

/// <summary>
/// Metadata phân trang dùng trong ApiResponse khi trả danh sách.
/// </summary>
public sealed class PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
