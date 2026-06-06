using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class OperationRequestStateMachine
{
    private static readonly IReadOnlyDictionary<OperationStatus, OperationStatus[]> Allowed = new Dictionary<OperationStatus, OperationStatus[]>
    {
        [OperationStatus.Draft] = [OperationStatus.Submitted, OperationStatus.Cancelled],
        [OperationStatus.Submitted] = [OperationStatus.InReview, OperationStatus.Approved, OperationStatus.Rejected, OperationStatus.Cancelled, OperationStatus.Draft],
        [OperationStatus.InReview] = [OperationStatus.Approved, OperationStatus.Rejected, OperationStatus.Cancelled, OperationStatus.Draft],
        [OperationStatus.Approved] = [OperationStatus.InProgress, OperationStatus.Cancelled],
        [OperationStatus.InProgress] = [OperationStatus.OnHold, OperationStatus.Completed, OperationStatus.Cancelled],
        [OperationStatus.OnHold] = [OperationStatus.InProgress, OperationStatus.Cancelled],
        [OperationStatus.Completed] = [OperationStatus.InProgress],
        [OperationStatus.Rejected] = [OperationStatus.Draft, OperationStatus.Cancelled],
        [OperationStatus.Cancelled] = [OperationStatus.InProgress]
    };

    public static bool CanTransition(OperationStatus from, OperationStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<OperationStatus> NextStates(OperationStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<OperationStatus>();
}
