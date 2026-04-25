using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class BudgetAdjustment
{
    public Guid Id { get; set; }

    public Guid BudgetId { get; set; }

    public string AdjustmentType { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal PreviousAmount { get; set; }

    public decimal NewAmount { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Budget Budget { get; set; } = null!;
}
