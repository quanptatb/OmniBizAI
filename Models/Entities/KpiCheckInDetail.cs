using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Detail line items within a KPI check-in (one per KPI detail/metric).</summary>
public class KpiCheckInDetail : TenantEntity
{
    public Guid KpiCheckInId { get; set; }
    public KpiCheckIn? KpiCheckIn { get; set; }

    [StringLength(255)]
    public string? MetricName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TargetValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AchievedValue { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }
}
