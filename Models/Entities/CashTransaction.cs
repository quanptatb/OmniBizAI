using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class CashTransaction : TenantEntity
{
    [StringLength(50)]
    public string TransactionNo { get; set; } = string.Empty;

    [StringLength(20)]
    public string TransactionType { get; set; } = "Expense"; // Income, Expense

    [StringLength(100)]
    public string Category { get; set; } = string.Empty; // E.g. "Bán hàng", "Lương", "Mua sắm", "Dịch vụ"

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateOnly TransactionDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [StringLength(100)]
    public string? PaymentMethod { get; set; } // Cash, BankTransfer, Card, Other

    [StringLength(250)]
    public string? ReferenceNo { get; set; } // Invoice, Receipt, Check number

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid? BudgetId { get; set; }
    public Budget? Budget { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public Guid RecordedByUserId { get; set; }
    public AppUser? RecordedByUser { get; set; }

    public CashTransactionStatus Status { get; set; } = CashTransactionStatus.Recorded;

    public Guid? ApprovedByUserId { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }
}
