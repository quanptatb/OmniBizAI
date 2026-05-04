// PR #23 — transaction boundary
using OmniBizAI.Models.Entities.Menus;

namespace OmniBizAI.Models.Entities.Procurement;

/// <summary>
/// Join table: một giấy đi chợ gom nhiều menu.
/// </summary>
public sealed class ProcurementPlanMenu
{
    public Guid Id { get; set; }
    public Guid ProcurementPlanId { get; set; }
    public Guid MenuPlanId { get; set; }

    // Navigation
    public ProcurementPlan ProcurementPlan { get; set; } = null!;
    public MenuPlan MenuPlan { get; set; } = null!;
}
