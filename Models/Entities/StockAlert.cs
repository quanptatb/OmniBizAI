using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class StockAlert : TenantEntity
{
    public Guid ProductServiceId { get; set; }
    public ProductService? ProductService { get; set; }

    [StringLength(50)]
    public string AlertType { get; set; } = "Low"; // Low, Critical, Overstock

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Threshold { get; set; } // ReorderPoint or SafetyStock or MaxStock

    [StringLength(500)]
    public string? Message { get; set; }

    public StockAlertStatus Status { get; set; } = StockAlertStatus.Active;

    public DateTimeOffset? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
    public AppUser? AcknowledgedByUser { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
}
