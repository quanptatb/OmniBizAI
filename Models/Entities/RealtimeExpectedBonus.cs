using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Real-time expected bonus for an employee based on current KPI performance.</summary>
public class RealtimeExpectedBonus : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid EvaluationPeriodId { get; set; }
    public EvaluationPeriod? EvaluationPeriod { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedBonus { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? CurrentScore { get; set; }

    [StringLength(50)]
    public string? EstimatedRank { get; set; }

    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
}
