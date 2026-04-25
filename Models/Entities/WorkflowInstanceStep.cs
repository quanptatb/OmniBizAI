using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class WorkflowInstanceStep
{
    public Guid Id { get; set; }

    public Guid InstanceId { get; set; }

    public int StepOrder { get; set; }

    public string StepName { get; set; } = null!;

    public Guid? AssignedTo { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? DeadlineAt { get; set; }

    public virtual ICollection<ApprovalAction> ApprovalActions { get; set; } = new List<ApprovalAction>();

    public virtual WorkflowInstance Instance { get; set; } = null!;
}
