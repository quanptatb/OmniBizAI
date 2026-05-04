// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Approvals;
using OmniBizAI.Models.Entities.Quantities;

namespace OmniBizAI.Models.Entities.Menus;

/// <summary>
/// Thực đơn theo ngày/ca/khách hàng. Core entity for menu planning workflow.
/// </summary>
public sealed class MenuPlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = "";

    public Guid CustomerCompanyId { get; set; }
    public Guid? ServiceContractId { get; set; }
    public Guid? DeliveryLocationId { get; set; }
    public Guid? PrimaryKitchenId { get; set; }

    public DateOnly ServiceDate { get; set; }
    public Guid MealShiftId { get; set; }
    public Guid? MealTypeId { get; set; }

    public int WeekNumber { get; set; }
    public int DayOfWeek { get; set; }
    public int MonthNumber { get; set; }
    public int WeekInMonth { get; set; }

    /// <summary>
    /// Status code stored as string — driven by StateDefinition/StateTransitionConfig.
    /// Examples: draft, internal_review, internal_approved, sent_to_customer,
    /// customer_approved, quantity_open, quantity_confirmed, bom_calculated, procurement_issued, cancelled.
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    public int RevisionNo { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset? InternalApprovedAt { get; set; }
    public DateTimeOffset? CustomerApprovedAt { get; set; }

    public Guid? ImportBatchId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [MaxLength(450)]
    public string? CreatedByUserId { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedByUserId { get; set; }

    // Navigation properties
    public ICollection<MenuPlanItem> Items { get; set; } = [];
    public ICollection<MenuPlanRevision> Revisions { get; set; } = [];
    public ICollection<InternalApproval> InternalApprovals { get; set; } = [];
    public ICollection<CustomerApprovalToken> CustomerApprovalTokens { get; set; } = [];
    public DailyMealOrder? DailyMealOrder { get; set; }
}
