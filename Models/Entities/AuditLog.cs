using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class AuditLog : TenantEntity
{
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(200)]
    public string UserName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(150)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Alias for EntityName used in some controllers/views.</summary>
    [NotMapped]
    public string EntityType { get => EntityName; set => EntityName = value; }

    public Guid? EntityId { get; set; }

    public string? OldValuesJson { get; set; }

    /// <summary>Alias for OldValuesJson.</summary>
    [NotMapped]
    public string? OldValues { get => OldValuesJson; set => OldValuesJson = value; }

    public string? NewValuesJson { get; set; }

    /// <summary>Alias for NewValuesJson.</summary>
    [NotMapped]
    public string? NewValues { get => NewValuesJson; set => NewValuesJson = value; }

    [StringLength(100)]
    public string? IpAddress { get; set; }
}
