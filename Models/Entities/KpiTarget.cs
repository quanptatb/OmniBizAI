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

    public ICollection<KpiResult> Results { get; set; } = new List<KpiResult>();
    public ICollection<KpiCheckIn> CheckIns { get; set; } = new List<KpiCheckIn>();
}
