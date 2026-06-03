using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Domain.StateMachines;

public static class SparePartRequisitionStateMachine
{
    private static readonly IReadOnlyDictionary<SparePartRequisitionStatus, SparePartRequisitionStatus[]> Allowed = new Dictionary<SparePartRequisitionStatus, SparePartRequisitionStatus[]>
    {
        [SparePartRequisitionStatus.Draft] = [SparePartRequisitionStatus.Submitted, SparePartRequisitionStatus.Cancelled],
        [SparePartRequisitionStatus.Submitted] = [SparePartRequisitionStatus.Approved, SparePartRequisitionStatus.Rejected, SparePartRequisitionStatus.Cancelled],
        [SparePartRequisitionStatus.Approved] = [SparePartRequisitionStatus.Issued, SparePartRequisitionStatus.Cancelled],
        [SparePartRequisitionStatus.Issued] = Array.Empty<SparePartRequisitionStatus>(),
        [SparePartRequisitionStatus.Rejected] = Array.Empty<SparePartRequisitionStatus>(),
        [SparePartRequisitionStatus.Cancelled] = Array.Empty<SparePartRequisitionStatus>()
    };

    public static bool CanTransition(SparePartRequisitionStatus from, SparePartRequisitionStatus to) =>
        from != to && Allowed.TryGetValue(from, out var nextStates) && nextStates.Contains(to);

    public static IReadOnlyList<SparePartRequisitionStatus> NextStates(SparePartRequisitionStatus from) =>
        Allowed.TryGetValue(from, out var nextStates) ? nextStates : Array.Empty<SparePartRequisitionStatus>();
}
