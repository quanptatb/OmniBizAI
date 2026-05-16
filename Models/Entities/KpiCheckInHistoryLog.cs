using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>History log tracking changes to a KPI check-in.</summary>
public class KpiCheckInHistoryLog : TenantEntity
{
    public Guid KpiCheckInId { get; set; }
    public KpiCheckIn? KpiCheckIn { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Details { get; set; }
}
