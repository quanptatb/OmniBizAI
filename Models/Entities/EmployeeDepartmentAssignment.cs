using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class EmployeeDepartmentAssignment : TenantEntity
{
    public Guid EmployeeProfileId { get; set; }
    public EmployeeProfile? EmployeeProfile { get; set; }

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
