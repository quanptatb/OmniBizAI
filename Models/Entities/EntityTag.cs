using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class EntityTag : TenantEntity
{
    public Guid TagId { get; set; }
    public Tag? Tag { get; set; }

    [StringLength(150)]
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }
}
