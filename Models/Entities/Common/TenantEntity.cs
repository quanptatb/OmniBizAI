using OmniBizAI.Models.Entities;

namespace OmniBizAI.Models.Entities.Common;

public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Tenant? Tenant { get; set; }
}
