using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class PlanTaskDependency : TenantEntity
{
    public Guid PlanId { get; set; }
    public OperationPlan? Plan { get; set; }

    public Guid PredecessorTaskId { get; set; }
    public PlanTask? PredecessorTask { get; set; }

    public Guid SuccessorTaskId { get; set; }
    public PlanTask? SuccessorTask { get; set; }

    public PlanTaskDependencyType Type { get; set; } = PlanTaskDependencyType.FinishToStart;
}
