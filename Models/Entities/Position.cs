using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class Position : TenantEntity
{
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public int Level { get; set; }

    public bool IsManagerial { get; set; }
}
