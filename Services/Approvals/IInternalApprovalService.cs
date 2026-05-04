// PR #23 — transaction boundary
namespace OmniBizAI.Services.Approvals;

using OmniBizAI.Services.Dtos;

/// <summary>
/// Internal approval workflow — multi-step approval for MenuPlan.
/// </summary>
public interface IInternalApprovalService
{
    Task<IReadOnlyList<InternalApprovalQueueItemDto>> GetQueueAsync(
        string approverUserId, CancellationToken ct = default);

    Task ApproveAsync(
        Guid approvalId, string actorUserId, string? comment, CancellationToken ct = default);

    Task RequestChangeAsync(
        Guid approvalId, string actorUserId, string comment, CancellationToken ct = default);
}
