using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class PaymentRequest : TenantEntity
{
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    [StringLength(50)]
    public string RequestNo { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; } = PaymentStatus.Draft;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public DateOnly? DueDate { get; set; }

    public ICollection<PaymentRequestLine> Lines { get; set; } = new List<PaymentRequestLine>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
