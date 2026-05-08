using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class KpiCheckIn : TenantEntity
{
    public Guid KpiTargetId { get; set; }
    public KpiTarget? KpiTarget { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column(TypeName = "decimal(18,2)")]
    public decimal ProgressValue { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
