using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkflowTransition : TenantEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    public Guid? FromStepId { get; set; }
    public WorkflowStep? FromStep { get; set; }

    public Guid ToStepId { get; set; }
    public WorkflowStep? ToStep { get; set; }

    [StringLength(80)]
    public string ActionCode { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ConditionExpression { get; set; }
}
