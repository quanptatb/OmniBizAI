using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class WorkOrderStateMachine
{
    private static readonly IReadOnlyDictionary<WorkOrderStatus, WorkOrderStatus[]> Allowed = new Dictionary<WorkOrderStatus, WorkOrderStatus[]>
    {
        [WorkOrderStatus.Open] = [WorkOrderStatus.Assigned, WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Assigned] = [WorkOrderStatus.InProgress, WorkOrderStatus.OnHold, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.InProgress] = [WorkOrderStatus.OnHold, WorkOrderStatus.Completed, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.OnHold] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Completed] = Array.Empty<WorkOrderStatus>(),
        [WorkOrderStatus.Cancelled] = Array.Empty<WorkOrderStatus>()
    };

    public static bool CanTransition(WorkOrderStatus from, WorkOrderStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<WorkOrderStatus> NextStates(WorkOrderStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<WorkOrderStatus>();

    public static bool IsTerminal(WorkOrderStatus status) =>
        status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled;
}
