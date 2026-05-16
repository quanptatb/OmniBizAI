using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class KpiCheckIn : TenantEntity
{
    public Guid KpiTargetId { get; set; }
    public KpiTarget? KpiTarget { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid? SubmittedByUserId { get; set; }
    public AppUser? SubmittedByUser { get; set; }

    public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset? DeadlineAt { get; set; }

    public bool IsLate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ProgressValue { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    // Fail reason
    public Guid? KpiFailReasonId { get; set; }
    public KpiFailReason? KpiFailReason { get; set; }

    // Review workflow
    public CheckInReviewStatus ReviewStatus { get; set; } = CheckInReviewStatus.Pending;

    public Guid? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    [StringLength(2000)]
    public string? ReviewComment { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? ReviewScore { get; set; }

    // Navigation
    public ICollection<KpiCheckInDetail> Details { get; set; } = new List<KpiCheckInDetail>();
    public ICollection<KpiCheckInHistoryLog> HistoryLogs { get; set; } = new List<KpiCheckInHistoryLog>();
}
