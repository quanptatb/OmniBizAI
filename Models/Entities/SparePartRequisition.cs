using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

/// <summary>Phiếu yêu cầu phụ tùng từ kỹ thuật viên gửi kho (F5.3)</summary>
public class SparePartRequisition : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    public SparePartRequisitionStatus Status { get; set; } = SparePartRequisitionStatus.Draft;

    public byte[] RowVersion { get; set; } = [];

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public Guid RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public Guid? LinkedWorkOrderId { get; set; }
    public WorkOrder? LinkedWorkOrder { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public AppUser? ApprovedByUser { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    public Guid? IssuedGoodsIssueId { get; set; }
    public GoodsIssue? IssuedGoodsIssue { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }

    /// <summary>Đánh dấu phiếu được hệ thống tự sinh khi tồn kho dưới ngưỡng</summary>
    public bool IsAutoReorder { get; set; }

    public ICollection<SparePartRequisitionLine> Lines { get; set; } = new List<SparePartRequisitionLine>();
}

public class SparePartRequisitionLine : TenantEntity
{
    public Guid RequisitionId { get; set; }
    public SparePartRequisition? Requisition { get; set; }

    public Guid SparePartId { get; set; }
    public SparePart? SparePart { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitCost { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
