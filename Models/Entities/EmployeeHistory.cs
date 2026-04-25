using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class EmployeeHistory
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string ChangeType { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public string? Reason { get; set; }

    public Guid? ChangedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
