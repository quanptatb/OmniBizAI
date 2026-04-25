using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class Budget
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid FiscalPeriodId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public decimal AllocatedAmount { get; set; }

    public decimal SpentAmount { get; set; }

    public decimal CommittedAmount { get; set; }

    public decimal WarningThreshold { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<BudgetAdjustment> BudgetAdjustments { get; set; } = new List<BudgetAdjustment>();

    public virtual BudgetCategory Category { get; set; } = null!;

    public virtual Company Company { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual FiscalPeriod FiscalPeriod { get; set; } = null!;

    public virtual ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
