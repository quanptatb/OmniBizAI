using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class UserRoleAssignment : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid RoleDefinitionId { get; set; }
    public RoleDefinition? RoleDefinition { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
