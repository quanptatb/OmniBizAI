using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class PlanTaskStateMachine
{
    private static readonly IReadOnlyDictionary<PlanTaskStatus, PlanTaskStatus[]> Allowed = new Dictionary<PlanTaskStatus, PlanTaskStatus[]>
    {
        [PlanTaskStatus.Todo] = [PlanTaskStatus.InProgress, PlanTaskStatus.Done, PlanTaskStatus.Delayed, PlanTaskStatus.Cancelled],
        [PlanTaskStatus.InProgress] = [PlanTaskStatus.Done, PlanTaskStatus.Delayed, PlanTaskStatus.Cancelled],
        [PlanTaskStatus.Delayed] = [PlanTaskStatus.InProgress, PlanTaskStatus.Done, PlanTaskStatus.Cancelled],
        [PlanTaskStatus.Done] = [PlanTaskStatus.InProgress],
        [PlanTaskStatus.Cancelled] = [PlanTaskStatus.Todo]
    };

    public static bool CanTransition(PlanTaskStatus from, PlanTaskStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<PlanTaskStatus> NextStates(PlanTaskStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<PlanTaskStatus>();
}
