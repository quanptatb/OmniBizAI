using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Assigns a KPI definition to a department (organization unit).</summary>
public class KpiDepartmentAssignment : TenantEntity
{
    public Guid KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }
}
