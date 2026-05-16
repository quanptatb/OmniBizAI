using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class OkrKeyResult : TenantEntity
{
    public Guid OkrObjectiveId { get; set; }
    public OkrObjective? OkrObjective { get; set; }

    [StringLength(500)]
    public string KeyResultName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Unit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentValue { get; set; }

    /// <summary>True if lower actual value means better result (e.g. reduce costs).</summary>
    public bool IsInverse { get; set; }

    [NotMapped]
    public decimal Progress
    {
        get
        {
            if (TargetValue == 0) return 0;
            decimal raw = IsInverse
                ? (TargetValue - CurrentValue) / TargetValue * 100
                : CurrentValue / TargetValue * 100;
            return Math.Clamp(Math.Round(raw, 2), 0, 100);
        }
    }
}
