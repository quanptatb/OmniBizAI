namespace OmniBizAI.Models.Common;

/// <summary>
/// Standard result type for service methods that don't return data.
/// Replaces inconsistent (bool, string) tuples.
/// </summary>
public record Result(bool Success, string? Message = null, string? ErrorCode = null)
{
    public static Result Ok(string? message = null) => new(true, message);
    public static Result Fail(string message, string? errorCode = null) => new(false, message, errorCode);
}

/// <summary>
/// Standard result type for service methods that return data.
/// </summary>
public record Result<T>(bool Success, T? Data = default, string? Message = null, string? ErrorCode = null)
    where T : class
{
    public static Result<T> Ok(T data, string? message = null) => new(true, data, message);
    public static Result<T> Fail(string message, string? errorCode = null) => new(false, default, message, errorCode);
}

/// <summary>
/// Result type for bulk operations — includes per-item success/failure details.
/// </summary>
public record BulkResult(bool Success, string? Message = null, int SuccessCount = 0, int FailureCount = 0, List<string>? Errors = null)
{
    public static BulkResult From(int successCount, int failureCount, List<string>? errors = null)
    {
        var msg = failureCount == 0
            ? $"Đã xử lý thành công {successCount} mục."
            : $"Đã xử lý {successCount}/{successCount + failureCount}. {failureCount} lỗi.";
        return new BulkResult(failureCount == 0, msg, successCount, failureCount, errors);
    }
}
