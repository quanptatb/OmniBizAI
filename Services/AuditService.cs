using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using OmniBizAI.Data;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.Services;

public interface IAuditService
{
    Task LogAsync(
        string entityType,
        Guid? entityId,
        string action,
        object? oldValueObj = null,
        object? newValueObj = null,
        object? extra = null,
        CancellationToken cancellationToken = default);
}

public class AuditService(ApplicationDbContext db, ITenantContext tenant, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task LogAsync(
        string entityType,
        Guid? entityId,
        string action,
        object? oldValueObj = null,
        object? newValueObj = null,
        object? extra = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        await db.AuditLogs.AddAsync(new AuditLog
        {
            TenantId = tenant.TenantId,
            UserId = tenant.UserId,
            UserName = tenant.UserFullName,
            Action = action,
            EntityName = entityType,
            EntityId = entityId,
            OldValuesJson = SerializeOrNull(oldValueObj),
            NewValuesJson = SerializeOrNull(newValueObj),
            ExtraJson = SerializeOrNull(extra),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            CorrelationId = httpContext?.TraceIdentifier,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private static string? SerializeOrNull(object? value)
    {
        if (value is null) return null;
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
