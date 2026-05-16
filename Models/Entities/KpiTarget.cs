using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class KpiTarget : TenantEntity
{
    public Guid KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public Guid? OwnerUserId { get; set; }
    public AppUser? OwnerUser { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TargetValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PassThreshold { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FailThreshold { get; set; }

    /// <summary>How often to check in (in days). E.g. 7 = weekly, 30 = monthly.</summary>
    public int? CheckInFrequencyDays { get; set; }

    /// <summary>Time of day for check-in deadline (e.g. 17:00).</summary>
    public TimeOnly? DeadlineTime { get; set; }

    /// <summary>Whether to send reminder notifications before deadline.</summary>
    public bool ReminderEnabled { get; set; }

    public ICollection<KpiResult> Results { get; set; } = new List<KpiResult>();
    public ICollection<KpiCheckIn> CheckIns { get; set; } = new List<KpiCheckIn>();
}
