// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniBizAI.Models.Entities.Bom;

/// <summary>
/// Định mức nguyên vật liệu cho 1 suất của một món.
/// Unique per (DishId, IngredientId).
/// </summary>
public sealed class DishBomItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DishId { get; set; }
    public Guid IngredientId { get; set; }

    [Column(TypeName = "decimal(18,3)")]
    public decimal QuantityPerServing { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal WasteRatePercent { get; set; }

    [MaxLength(100)]
    public string? SourceExternalId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
