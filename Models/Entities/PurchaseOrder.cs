using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class PurchaseOrder : TenantEntity
{
    public Guid? ProcurementRequestId { get; set; }
    public ProcurementRequest? ProcurementRequest { get; set; }

    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [StringLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAmount { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}
