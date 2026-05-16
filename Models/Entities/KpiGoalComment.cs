using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Comments on a KPI goal or check-in (by manager, director, or employee).</summary>
public class KpiGoalComment : TenantEntity
{
    public Guid? KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public Guid? KpiCheckInId { get; set; }
    public KpiCheckIn? KpiCheckIn { get; set; }

    public Guid CommenterId { get; set; }
    public AppUser? Commenter { get; set; }

    [StringLength(3000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(50)]
    public string? CommentType { get; set; }
}
