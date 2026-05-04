// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Configuration;

/// <summary>
/// Gán role cho user theo tenant. Dùng để check quyền duyệt.
/// </summary>
public sealed class UserRoleAssignment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; } = "";

    public Guid RoleDefinitionId { get; set; }

    /// <summary>
    /// Scope type: "Global", "Department", "DeliveryLocation".
    /// </summary>
    [MaxLength(50)]
    public string? ScopeType { get; set; }

    public Guid? ScopeId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
