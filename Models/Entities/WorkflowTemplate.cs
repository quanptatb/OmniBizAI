using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class WorkflowTemplate
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string? Description { get; set; }

    public int Version { get; set; }

    public bool IsActive { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<WorkflowCondition> WorkflowConditions { get; set; } = new List<WorkflowCondition>();

    public virtual ICollection<WorkflowInstance> WorkflowInstances { get; set; } = new List<WorkflowInstance>();

    public virtual ICollection<WorkflowStep> WorkflowSteps { get; set; } = new List<WorkflowStep>();
}
