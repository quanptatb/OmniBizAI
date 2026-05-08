using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class PermissionAssignment : TenantEntity
{
    public Guid RoleDefinitionId { get; set; }
    public RoleDefinition? RoleDefinition { get; set; }

    public Guid PermissionDefinitionId { get; set; }
    public PermissionDefinition? PermissionDefinition { get; set; }

    public bool IsGranted { get; set; } = true;
}
