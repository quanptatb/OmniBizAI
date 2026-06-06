using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class WorkItemStateMachine
{
    private static readonly IReadOnlyDictionary<WorkItemStatus, WorkItemStatus[]> Allowed = new Dictionary<WorkItemStatus, WorkItemStatus[]>
    {
        [WorkItemStatus.Todo] = [WorkItemStatus.InProgress, WorkItemStatus.Blocked, WorkItemStatus.Done, WorkItemStatus.Cancelled],
        [WorkItemStatus.InProgress] = [WorkItemStatus.Todo, WorkItemStatus.Blocked, WorkItemStatus.Done, WorkItemStatus.Cancelled],
        [WorkItemStatus.Blocked] = [WorkItemStatus.Todo, WorkItemStatus.InProgress, WorkItemStatus.Cancelled],
        [WorkItemStatus.Done] = [WorkItemStatus.InProgress],
        [WorkItemStatus.Cancelled] = [WorkItemStatus.Todo, WorkItemStatus.InProgress]
    };

    public static bool CanTransition(WorkItemStatus from, WorkItemStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<WorkItemStatus> NextStates(WorkItemStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<WorkItemStatus>();
}
