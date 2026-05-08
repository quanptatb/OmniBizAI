using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class UserSession : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(200)]
    public string SessionKey { get; set; } = string.Empty;

    [StringLength(100)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
