using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Tracks adjustments made to a KPI definition (target changes, weight changes, etc.).</summary>
public class KpiAdjustmentHistory : TenantEntity
{
    public Guid KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public Guid AdjusterUserId { get; set; }
    public AppUser? AdjusterUser { get; set; }

    [StringLength(100)]
    public string FieldChanged { get; set; } = string.Empty;

    [StringLength(500)]
    public string? OldValue { get; set; }

    [StringLength(500)]
    public string? NewValue { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }
}
