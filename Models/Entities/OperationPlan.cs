using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class OperationPlan : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string PlanType { get; set; } = "Daily"; // Daily, Weekly, Monthly

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [Required, StringLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Approved, InProgress, Completed, Cancelled

    public string? Notes { get; set; }

    public ICollection<PlanTask> Tasks { get; set; } = new List<PlanTask>();

    // Liên kết với OKR
    public Guid? OkrObjectiveId { get; set; }
    public OkrObjective? OkrObjective { get; set; }

    // Liên kết với KPI
    public Guid? KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }
}
