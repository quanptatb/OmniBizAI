using System.Diagnostics;
using OmniBizAI.Application.Common;

namespace OmniBizAI.WebAPI.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var status = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                BusinessRuleException => StatusCodes.Status422UnprocessableEntity,
                AppException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            if (status >= 500)
            {
                _logger.LogError(ex, "Unhandled request failure {TraceId}", traceId);
            }
            else
            {
                _logger.LogWarning(ex, "Handled request failure {TraceId}", traceId);
            }

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = status >= 500 ? "Internal server error" : ex.Message,
                traceId
            });
        }
    }
}
