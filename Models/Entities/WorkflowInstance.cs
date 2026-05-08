using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class WorkflowInstance : TenantEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    [StringLength(150)]
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public Guid? CurrentStepId { get; set; }
    public WorkflowStep? CurrentStep { get; set; }

    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Running;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<WorkflowHistory> HistoryEntries { get; set; } = new List<WorkflowHistory>();
    public ICollection<ApprovalTask> ApprovalTasks { get; set; } = new List<ApprovalTask>();
}
