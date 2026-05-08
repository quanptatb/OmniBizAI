using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class AppUser : TenantEntity
{
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    public UserProfile? Profile { get; set; }
    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = new List<UserRoleAssignment>();
}
