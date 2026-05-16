using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Assigns a KPI definition to an individual employee/user with weight.</summary>
public class KpiEmployeeAssignment : TenantEntity
{
    public Guid KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; } = 100;
}
