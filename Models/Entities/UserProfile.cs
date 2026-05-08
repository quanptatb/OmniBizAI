using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class UserProfile : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    [StringLength(100)]
    public string? TimeZoneId { get; set; }

    [StringLength(10)]
    public string? Locale { get; set; }
}
