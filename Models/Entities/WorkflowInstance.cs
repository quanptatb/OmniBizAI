using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class WorkflowInstance
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public string EntityType { get; set; } = null!;

    public Guid EntityId { get; set; }

    public int CurrentStepOrder { get; set; }

    public int TotalSteps { get; set; }

    public string Status { get; set; } = null!;

    public Guid? InitiatedBy { get; set; }

    public DateTime InitiatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string MetadataJson { get; set; } = null!;

    public virtual WorkflowTemplate Template { get; set; } = null!;

    public virtual ICollection<WorkflowInstanceStep> WorkflowInstanceSteps { get; set; } = new List<WorkflowInstanceStep>();
}
