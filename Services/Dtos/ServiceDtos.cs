// PR #23 — transaction boundary
namespace OmniBizAI.Services.Dtos;

/// <summary>
/// Request DTO for customer approval submission (from email link).
/// </summary>
public sealed class CustomerApprovalSubmitRequest
{
    public string Token { get; init; } = "";
    public string DecisionActionCode { get; init; } = ""; // "approve" or "request_change"
    public int? ExpectedQtyInput { get; init; }
    public int? FinalQtyInput { get; init; }
    public int? ExtraInputQty { get; init; }
    public string ExtraModeCode { get; init; } = "";
    public string? Comment { get; init; }
}

/// <summary>
/// ViewModel for customer reviewing the menu via email token link.
/// </summary>
public sealed class CustomerApprovalReviewViewModel
{
    public string Token { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateOnly ServiceDate { get; set; }
    public string MealShiftName { get; set; } = "";
    public IReadOnlyList<string> DishNames { get; set; } = [];
    public int? ExpectedQty { get; set; }
    public int? FinalQty { get; set; }
    public int? ExtraInputQty { get; set; }
    public string ExtraModeCode { get; set; } = "";
    public string? Comment { get; set; }
}

/// <summary>
/// DTO for internal approval queue item.
/// </summary>
public sealed class InternalApprovalQueueItemDto
{
    public Guid ApprovalId { get; set; }
    public Guid MenuPlanId { get; set; }
    public string MenuPlanCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public DateOnly ServiceDate { get; set; }
    public string MealShiftName { get; set; } = "";
    public string StepName { get; set; } = "";
    public string RequiredRoleDisplayName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// DTO for procurement plan preview line.
/// </summary>
public sealed class ProcurementLineDto
{
    public Guid IngredientId { get; init; }
    public string IngredientName { get; init; } = "";
    public string Unit { get; init; } = "";
    public decimal RequiredQty { get; init; }
    public decimal WasteQty { get; init; }
    public decimal PurchaseQty { get; init; }
    public string SourceSummary { get; init; } = "";
}

/// <summary>
/// Request DTO for generating a procurement plan.
/// </summary>
public sealed class GenerateProcurementPlanRequest
{
    public Guid TenantId { get; init; }
    public DateOnly ServiceDate { get; init; }
    public Guid MealShiftId { get; init; }
    public IReadOnlyList<Guid> MenuPlanIds { get; init; } = [];
}
