using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class Transaction
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string TransactionNumber { get; set; } = null!;

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public Guid WalletId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid? BudgetId { get; set; }

    public Guid? PaymentRequestId { get; set; }

    public Guid? VendorId { get; set; }

    public DateOnly TransactionDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public bool Reconciled { get; set; }

    public Guid? RecordedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Budget? Budget { get; set; }

    public virtual BudgetCategory Category { get; set; } = null!;

    public virtual Company Company { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual PaymentRequest? PaymentRequest { get; set; }

    public virtual Vendor? Vendor { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
