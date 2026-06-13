using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class MaintenanceIncidentStateMachine
{
    private static readonly IReadOnlyDictionary<IncidentStatus, IncidentStatus[]> Allowed = new Dictionary<IncidentStatus, IncidentStatus[]>
    {
        [IncidentStatus.Open] = [IncidentStatus.Investigating, IncidentStatus.InProgress, IncidentStatus.Resolved, IncidentStatus.Closed],
        [IncidentStatus.Investigating] = [IncidentStatus.InProgress, IncidentStatus.Resolved, IncidentStatus.Closed],
        [IncidentStatus.InProgress] = [IncidentStatus.Resolved, IncidentStatus.Closed],
        [IncidentStatus.Resolved] = [IncidentStatus.Closed, IncidentStatus.Reopened],
        [IncidentStatus.Closed] = [IncidentStatus.Reopened],
        [IncidentStatus.Reopened] = [IncidentStatus.Investigating, IncidentStatus.InProgress, IncidentStatus.Resolved, IncidentStatus.Closed]
    };

    public static bool CanTransition(IncidentStatus from, IncidentStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<IncidentStatus> NextStates(IncidentStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<IncidentStatus>();
}
