using OmniBizAI.Domain.Common;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Rules;

namespace OmniBizAI.Domain.Entities.Finance;

public sealed class FiscalPeriod : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Monthly";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class BudgetCategory : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public BudgetCategory? Parent { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BudgetCategory> Children { get; set; } = new List<BudgetCategory>();
}

public sealed class Budget : SoftDeletableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public BudgetCategory Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal WarningThreshold { get; set; } = 80;
    public BudgetStatus Status { get; set; } = BudgetStatus.Active;
    public string? Notes { get; set; }
    public decimal RemainingAmount => BudgetRules.Remaining(AllocatedAmount, SpentAmount, CommittedAmount);
    public decimal UtilizationPercent => BudgetRules.UtilizationPercent(AllocatedAmount, SpentAmount);
    public string WarningLevel => BudgetRules.WarningLevel(AllocatedAmount, SpentAmount, WarningThreshold);
}

public sealed class BudgetAdjustment : BaseEntity
{
    public Guid BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;
    public string AdjustmentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal NewAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Vendor : SoftDeletableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BankAccount { get; set; }
    public decimal? Rating { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class Wallet : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "BankAccount";
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "VND";
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class PaymentRequest : SoftDeletableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid RequesterId { get; set; }
    public Employee Requester { get; set; } = null!;
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public Guid? BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public Guid CategoryId { get; set; }
    public BudgetCategory Category { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string? PaymentMethod { get; set; }
    public DateOnly? PaymentDueDate { get; set; }
    public string Priority { get; set; } = "Normal";
    public PaymentRequestStatus Status { get; set; } = PaymentRequestStatus.Draft;
    public decimal? AiRiskScore { get; set; }
    public string? AiRiskFlagsJson { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public ICollection<PaymentRequestItem> Items { get; set; } = new List<PaymentRequestItem>();
}

public sealed class PaymentRequestItem : BaseEntity
{
    public Guid PaymentRequestId { get; set; }
    public PaymentRequest PaymentRequest { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PaymentRequestAttachment : BaseEntity
{
    public Guid PaymentRequestId { get; set; }
    public PaymentRequest PaymentRequest { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Guid? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Transaction : SoftDeletableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string TransactionNumber { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public BudgetCategory Category { get; set; } = null!;
    public Guid? BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public Guid? PaymentRequestId { get; set; }
    public PaymentRequest? PaymentRequest { get; set; }
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Completed";
    public bool Reconciled { get; set; }
    public Guid? RecordedBy { get; set; }
}
