using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class KpiResult : TenantEntity
{
    public Guid KpiTargetId { get; set; }
    public KpiTarget? KpiTarget { get; set; }

    public Guid? OwnerUserId { get; set; }
    public AppUser? OwnerUser { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Score { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}
