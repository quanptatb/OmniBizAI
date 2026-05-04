// PR #23 — transaction boundary
namespace OmniBizAI.Services.Procurement;

using OmniBizAI.Models.Entities.Procurement;
using OmniBizAI.Services.Dtos;

/// <summary>
/// Procurement plan (giấy đi chợ) — generate, preview, and issue.
/// </summary>
public interface IProcurementPlanService
{
    Task<IReadOnlyList<ProcurementLineDto>> PreviewAsync(
        GenerateProcurementPlanRequest request, CancellationToken ct = default);

    Task<ProcurementPlan> GenerateAsync(
        GenerateProcurementPlanRequest request, string actorUserId, CancellationToken ct = default);

    Task IssueAsync(Guid procurementPlanId, string actorUserId, CancellationToken ct = default);
}
