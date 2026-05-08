using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class TenantModule : TenantEntity
{
    [StringLength(80)]
    public string ModuleCode { get; set; } = string.Empty;

    [StringLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    public ModuleStatus Status { get; set; } = ModuleStatus.Enabled;

    public DateOnly? EnabledFrom { get; set; }

    public DateOnly? EnabledTo { get; set; }
}
