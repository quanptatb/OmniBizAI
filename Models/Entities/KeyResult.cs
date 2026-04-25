using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class KeyResult
{
    public Guid Id { get; set; }

    public Guid ObjectiveId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string MetricType { get; set; } = null!;

    public string? Unit { get; set; }

    public decimal StartValue { get; set; }

    public decimal TargetValue { get; set; }

    public decimal CurrentValue { get; set; }

    public decimal Progress { get; set; }

    public decimal Weight { get; set; }

    public string Direction { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid? AssigneeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual Employee? Assignee { get; set; }

    public virtual Objective Objective { get; set; } = null!;
}
