using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

/// <summary>Final evaluation result for an employee in a given period.</summary>
public class EvaluationResult : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid EvaluationPeriodId { get; set; }
    public EvaluationPeriod? EvaluationPeriod { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? TotalScore { get; set; }

    public Guid? GradingRankId { get; set; }
    public GradingRank? GradingRank { get; set; }

    [StringLength(50)]
    public string? Classification { get; set; }

    [StringLength(2000)]
    public string? ReviewComment { get; set; }

    public EvaluationSubmissionStatus SubmissionStatus { get; set; } = EvaluationSubmissionStatus.Draft;

    public Guid? SubmittedByUserId { get; set; }
    public AppUser? SubmittedByUser { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }

    public Guid? DirectorReviewedByUserId { get; set; }
    public AppUser? DirectorReviewedByUser { get; set; }
    public DateTimeOffset? DirectorReviewedAt { get; set; }

    [StringLength(2000)]
    public string? DirectorReviewComment { get; set; }
}
