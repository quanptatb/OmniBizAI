using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class GoodsReceipt : TenantEntity
{
    [StringLength(50)]
    public string ReceiptNo { get; set; } = string.Empty;

    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid ReceivedByUserId { get; set; }
    public AppUser? ReceivedByUser { get; set; }

    public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

    [StringLength(500)]
    public string? WarehouseLocation { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }

    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}
