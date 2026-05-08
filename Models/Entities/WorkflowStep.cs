using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkflowStep : TenantEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    public string StepType { get; set; } = "Approval";

    [StringLength(80)]
    public string? AssignedRole { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; } = true;
}
