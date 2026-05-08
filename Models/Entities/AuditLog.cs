using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class AuditLog : TenantEntity
{
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(150)]
    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    [StringLength(100)]
    public string? IpAddress { get; set; }
}
