using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class PaymentRequest
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string RequestNumber { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid RequesterId { get; set; }

    public Guid? VendorId { get; set; }

    public Guid? BudgetId { get; set; }

    public Guid CategoryId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public DateOnly? PaymentDueDate { get; set; }

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal? AiRiskScore { get; set; }

    public string? AiRiskFlagsJson { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? RejectionReason { get; set; }

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

    public virtual ICollection<PaymentRequestAttachment> PaymentRequestAttachments { get; set; } = new List<PaymentRequestAttachment>();

    public virtual ICollection<PaymentRequestItem> PaymentRequestItems { get; set; } = new List<PaymentRequestItem>();

    public virtual Employee Requester { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual Vendor? Vendor { get; set; }
}
