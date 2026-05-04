// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniBizAI.Models.Entities.Procurement;

/// <summary>
/// Dòng nguyên liệu cần mua trong giấy đi chợ.
/// </summary>
public sealed class ProcurementPlanLine
{
    public Guid Id { get; set; }
    public Guid ProcurementPlanId { get; set; }
    public Guid IngredientId { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal RequiredQty { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal WasteQty { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal PurchaseQty { get; set; }

    [MaxLength(30)]
    public string Unit { get; set; } = "";

    [MaxLength(500)]
    public string SourceSummary { get; set; } = "";

    // Navigation
    public ProcurementPlan ProcurementPlan { get; set; } = null!;
}
