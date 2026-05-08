using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class Tag : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Color { get; set; }

    public ICollection<EntityTag> EntityTags { get; set; } = new List<EntityTag>();
}
