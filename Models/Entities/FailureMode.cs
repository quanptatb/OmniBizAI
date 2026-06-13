using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

/// <summary>Danh mục mode lỗi để phân tích RCA (F5.6)</summary>
public class FailureMode : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public FailureModeCategory Category { get; set; } = FailureModeCategory.Mechanical;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(2000)]
    public string? TypicalPreventionMeasure { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MaintenanceIncident> Incidents { get; set; } = new List<MaintenanceIncident>();
}
