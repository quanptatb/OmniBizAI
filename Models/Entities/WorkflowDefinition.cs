using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkflowDefinition : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    public string TargetEntityName { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
    public ICollection<WorkflowInstance> Instances { get; set; } = new List<WorkflowInstance>();
}
