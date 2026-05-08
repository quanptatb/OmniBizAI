using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class UserTenant : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public bool IsDefault { get; set; }

    public DateTimeOffset? LastAccessedAt { get; set; }
}
