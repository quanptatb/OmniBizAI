using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class Vendor : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TaxCode { get; set; }

    [StringLength(255)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();
}
