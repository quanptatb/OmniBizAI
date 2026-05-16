using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Compares KPI target vs achieved result for an employee in a given period.</summary>
public class KpiResultComparison : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public Guid EvaluationPeriodId { get; set; }
    public EvaluationPeriod? EvaluationPeriod { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AchievedValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? CompletionPercent { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
