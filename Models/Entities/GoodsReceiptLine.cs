using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class GoodsReceiptLine : TenantEntity
{
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }

    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }

    public Guid? ProductServiceId { get; set; }
    public ProductService? ProductService { get; set; }

    [StringLength(250)]
    public string? ItemName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OrderedQuantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ReceivedQuantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RejectedQuantity { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public bool QualityPassed { get; set; } = true;
}
