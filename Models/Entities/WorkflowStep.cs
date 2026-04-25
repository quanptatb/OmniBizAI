using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class WorkflowStep
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public int StepOrder { get; set; }

    public string Name { get; set; } = null!;

    public string ApproverType { get; set; } = null!;

    public Guid? ApproverRoleId { get; set; }

    public Guid? ApproverUserId { get; set; }

    public bool IsRequired { get; set; }

    public bool CanDelegate { get; set; }

    public int TimeoutHours { get; set; }

    public virtual WorkflowTemplate Template { get; set; } = null!;
}
