using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class KpiDefinition : TenantEntity
{
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;

    public KpiOwnerType OwnerType { get; set; } = KpiOwnerType.Department;

    public KpiPeriodType PeriodType { get; set; } = KpiPeriodType.Monthly;

    public bool IsActive { get; set; } = true;

    public ICollection<KpiTarget> Targets { get; set; } = new List<KpiTarget>();
}
