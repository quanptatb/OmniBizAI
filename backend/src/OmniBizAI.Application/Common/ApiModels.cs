namespace OmniBizAI.Application.Common;

public sealed record ApiError(string Field, string Message);

public sealed record ApiResponse<T>(bool Success, T? Data, string Message, IReadOnlyCollection<ApiError>? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string message = "Operation successful") => new(true, data, message);
    public static ApiResponse<T> Fail(string message, IReadOnlyCollection<ApiError>? errors = null) => new(false, default, message, errors);
}

public sealed record PagedRequest(int Page = 1, int PageSize = 20, string? Search = null, string? SortBy = null, string? SortOrder = "desc")
{
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize is < 1 or > 100 ? 20 : PageSize;
}

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

    public static PagedResult<T> Create(IEnumerable<T> source, PagedRequest request)
    {
        var materialized = source.ToList();
        var page = request.SafePage;
        var pageSize = request.SafePageSize;
        var items = materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T>(items, page, pageSize, materialized.Count);
    }
}

public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
