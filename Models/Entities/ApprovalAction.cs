using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class ApprovalAction
{
    public Guid Id { get; set; }

    public Guid InstanceStepId { get; set; }

    public Guid InstanceId { get; set; }

    public Guid UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Comment { get; set; }

    public DateTime ActionAt { get; set; }

    public string? IpAddress { get; set; }

    public virtual WorkflowInstanceStep InstanceStep { get; set; } = null!;
}
