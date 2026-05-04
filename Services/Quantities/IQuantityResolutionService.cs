// PR #23 — transaction boundary
namespace OmniBizAI.Services.Quantities;

/// <summary>
/// Resolves quantity (ExpectedQty, FinalQty, TotalCookingQty) using fallback rules.
/// Implementation is out of scope for this PR — interface only.
/// </summary>
public interface IQuantityResolutionService
{
    /// <summary>
    /// Resolves quantity based on customer input, previous-day fallback, and contract defaults.
    /// Returns null if mandatory fallback data is missing (no previous day, no contract default).
    /// </summary>
    Task<ResolvedQuantityResult?> ResolveAsync(
        ResolveQuantityRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ResolveQuantityRequest
{
    public Guid MenuPlanId { get; init; }
    public int? ExpectedQtyInput { get; init; }
    public int? FinalQtyInput { get; init; }
    public int? ExtraInputQty { get; init; }
    public string ExtraModeCode { get; init; } = "";
    public string? Note { get; init; }
}

public sealed class ResolvedQuantityResult
{
    public int ExpectedQty { get; init; }
    public string ExpectedQtySourceCode { get; init; } = "";
    public int FinalQty { get; init; }
    public string FinalQtySourceCode { get; init; } = "";
    public int ExtraInputQty { get; init; }
    public string ExtraModeCode { get; init; } = "";
    public int TotalCookingQty { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
