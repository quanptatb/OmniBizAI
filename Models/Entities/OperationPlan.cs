using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

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
    public DateTime? ProjectedEndDate { get; set; }

    public Guid? SourceOperationRequestId { get; set; }
    public OperationRequest? SourceOperationRequest { get; set; }

    public OperationPlanStatus Status { get; set; } = OperationPlanStatus.Draft;

    public byte[] RowVersion { get; set; } = [];

    public string? Notes { get; set; }

    public ICollection<PlanTask> Tasks { get; set; } = new List<PlanTask>();
    // Liên kết với OKR
    public Guid? OkrObjectiveId { get; set; }
    public OkrObjective? OkrObjective { get; set; }

    // Liên kết với KPI
    public Guid? KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    public ICollection<PlanTaskBaseline> TaskBaselines { get; set; } = new List<PlanTaskBaseline>();
    public ICollection<PlanChangeOrder> ChangeOrders { get; set; } = new List<PlanChangeOrder>();
    public ICollection<PlanTaskDependency> TaskDependencies { get; set; } = new List<PlanTaskDependency>();
}
