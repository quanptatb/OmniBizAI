using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class Tenant : BaseEntity
{
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? BusinessType { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public ICollection<TenantSetting> Settings { get; set; } = new List<TenantSetting>();
    public ICollection<TenantModule> Modules { get; set; } = new List<TenantModule>();
    public ICollection<BusinessProfile> BusinessProfiles { get; set; } = new List<BusinessProfile>();
    public ICollection<SystemParameter> SystemParameters { get; set; } = new List<SystemParameter>();
    public ICollection<NumberSequence> NumberSequences { get; set; } = new List<NumberSequence>();
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<OrganizationUnit> OrganizationUnits { get; set; } = new List<OrganizationUnit>();
}
