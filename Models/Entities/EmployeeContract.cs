using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class EmployeeContract : TenantEntity
{
    public Guid EmployeeProfileId { get; set; }
    public EmployeeProfile? EmployeeProfile { get; set; }

    [StringLength(80)]
    public string ContractNo { get; set; } = string.Empty;

    [StringLength(80)]
    public string ContractType { get; set; } = string.Empty;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
