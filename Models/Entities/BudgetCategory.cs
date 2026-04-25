using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class BudgetCategory
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Description { get; set; }

    public string? Color { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<BudgetCategory> InverseParent { get; set; } = new List<BudgetCategory>();

    public virtual BudgetCategory? Parent { get; set; }

    public virtual ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
