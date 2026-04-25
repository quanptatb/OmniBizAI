using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class WorkflowCondition
{
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }

    public string Field { get; set; } = null!;

    public string Operator { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string ThenAction { get; set; } = null!;

    public int? ThenStepOrder { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; }

    public virtual WorkflowTemplate Template { get; set; } = null!;
}
