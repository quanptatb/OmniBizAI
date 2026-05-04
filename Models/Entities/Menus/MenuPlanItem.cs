// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Menus;

/// <summary>
/// Một vị trí món trong thực đơn. Unique per (MenuPlanId, MealSlotDefinitionId).
/// </summary>
public sealed class MenuPlanItem
{
    public Guid Id { get; set; }
    public Guid MenuPlanId { get; set; }
    public Guid MealSlotDefinitionId { get; set; }
    public Guid DishId { get; set; }
    public Guid? KitchenId { get; set; }
    public int SortOrder { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    // Navigation
    public MenuPlan MenuPlan { get; set; } = null!;
}
