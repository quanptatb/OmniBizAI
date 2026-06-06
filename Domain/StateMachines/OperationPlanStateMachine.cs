using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class OperationPlanStateMachine
{
    private static readonly IReadOnlyDictionary<OperationPlanStatus, OperationPlanStatus[]> Allowed = new Dictionary<OperationPlanStatus, OperationPlanStatus[]>
    {
        [OperationPlanStatus.Draft] = [OperationPlanStatus.Submitted, OperationPlanStatus.Cancelled],
        [OperationPlanStatus.Submitted] = [OperationPlanStatus.Approved, OperationPlanStatus.Draft, OperationPlanStatus.Cancelled],
        [OperationPlanStatus.Approved] = [OperationPlanStatus.InProgress, OperationPlanStatus.Cancelled],
        [OperationPlanStatus.InProgress] = [OperationPlanStatus.Completed, OperationPlanStatus.Cancelled]
    };

    public static bool CanTransition(OperationPlanStatus from, OperationPlanStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<OperationPlanStatus> NextStates(OperationPlanStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<OperationPlanStatus>();
}
