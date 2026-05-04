// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Menus;

namespace OmniBizAI.Models.Entities.Quantities;

/// <summary>
/// Số lượng suất ăn chính cho một MenuPlan. Unique per MenuPlanId.
/// </summary>
public sealed class DailyMealOrder
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MenuPlanId { get; set; }
    public Guid CustomerCompanyId { get; set; }
    public Guid? ServiceContractId { get; set; }
    public Guid? DeliveryLocationId { get; set; }

    public DateOnly ServiceDate { get; set; }
    public Guid MealShiftId { get; set; }
    public Guid? MealTypeId { get; set; }

    public int ExpectedQty { get; set; }

    [MaxLength(50)]
    public string ExpectedQtySourceCode { get; set; } = "";

    public int FinalQty { get; set; }

    [MaxLength(50)]
    public string FinalQtySourceCode { get; set; } = "";

    public int ExtraInputQty { get; set; }

    [MaxLength(50)]
    public string ExtraModeCode { get; set; } = "none";

    public int TotalCookingQty { get; set; }

    [MaxLength(500)]
    public string? ResolveNote { get; set; }

    public bool IsLocked { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public MenuPlan MenuPlan { get; set; } = null!;
    public ICollection<QuantitySubmission> Submissions { get; set; } = [];
}
