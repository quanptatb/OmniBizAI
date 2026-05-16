using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Allocates an OKR objective to a department (organization unit).</summary>
public class OkrDepartmentAllocation : TenantEntity
{
    public Guid OkrObjectiveId { get; set; }
    public OkrObjective? OkrObjective { get; set; }

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }
}
