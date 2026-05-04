// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Audit;

/// <summary>
/// Nhật ký thay đổi — ghi mỗi action nghiệp vụ trong cùng transaction.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    /// <summary>
    /// Action code, e.g. "internal_approve", "customer_approve", "procurement_issue".
    /// </summary>
    [MaxLength(100)]
    public string Action { get; set; } = "";

    [MaxLength(100)]
    public string EntityType { get; set; } = "";

    public Guid EntityId { get; set; }

    /// <summary>
    /// JSON serialization of old values (before change).
    /// </summary>
    public string? OldValuesJson { get; set; }

    /// <summary>
    /// JSON serialization of new values (after change).
    /// </summary>
    public string? NewValuesJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
