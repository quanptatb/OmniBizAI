using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class ApprovalTaskStateMachine
{
    private static readonly IReadOnlyDictionary<ApprovalStatus, ApprovalStatus[]> Allowed = new Dictionary<ApprovalStatus, ApprovalStatus[]>
    {
        [ApprovalStatus.Pending] = [ApprovalStatus.Approved, ApprovalStatus.Rejected, ApprovalStatus.Skipped, ApprovalStatus.Cancelled]
    };

    public static bool CanTransition(ApprovalStatus from, ApprovalStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<ApprovalStatus> NextStates(ApprovalStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<ApprovalStatus>();
}
