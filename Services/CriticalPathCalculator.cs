using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Services;

public sealed record CriticalPathTaskResult(
    Guid TaskId,
    DateTime EarlyStart,
    DateTime EarlyFinish,
    DateTime LateStart,
    DateTime LateFinish,
    int SlackMinutes,
    bool IsCritical);

public sealed record CriticalPathResult(
    bool HasCycle,
    string? Error,
    DateTime? ProjectedEndDate,
    IReadOnlyDictionary<Guid, CriticalPathTaskResult> Tasks);

public class CriticalPathCalculator
{
    public CriticalPathResult Calculate(
        IReadOnlyCollection<PlanTask> tasks,
        IReadOnlyCollection<PlanTaskDependency> dependencies,
        DateTime? referenceTime = null)
    {
        var activeTasks = tasks
            .Where(t => !t.IsDeleted && t.Status != PlanTaskStatus.Cancelled)
            .OrderBy(t => t.StartTime)
            .ToList();

        if (!activeTasks.Any())
        {
            return new CriticalPathResult(false, null, null, new Dictionary<Guid, CriticalPathTaskResult>());
        }

        var taskById = activeTasks.ToDictionary(t => t.Id);
        var activeTaskIds = taskById.Keys.ToHashSet();
        var activeDependencies = dependencies
            .Where(d => !d.IsDeleted
                && activeTaskIds.Contains(d.PredecessorTaskId)
                && activeTaskIds.Contains(d.SuccessorTaskId)
                && d.PredecessorTaskId != d.SuccessorTaskId)
            .ToList();

        var outgoing = activeTaskIds.ToDictionary(id => id, _ => new List<PlanTaskDependency>());
        var incomingCount = activeTaskIds.ToDictionary(id => id, _ => 0);
        foreach (var dependency in activeDependencies)
        {
            outgoing[dependency.PredecessorTaskId].Add(dependency);
            incomingCount[dependency.SuccessorTaskId]++;
        }

        var queue = new Queue<Guid>(incomingCount
            .Where(x => x.Value == 0)
            .OrderBy(x => taskById[x.Key].StartTime)
            .Select(x => x.Key));

        var orderedIds = new List<Guid>();
        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            orderedIds.Add(currentId);

            foreach (var dependency in outgoing[currentId])
            {
                incomingCount[dependency.SuccessorTaskId]--;
                if (incomingCount[dependency.SuccessorTaskId] == 0)
                {
                    queue.Enqueue(dependency.SuccessorTaskId);
                }
            }
        }

        if (orderedIds.Count != activeTasks.Count)
        {
            return new CriticalPathResult(
                true,
                "Phụ thuộc công việc đang tạo vòng lặp, không thể tính critical path.",
                null,
                new Dictionary<Guid, CriticalPathTaskResult>());
        }

        var now = referenceTime ?? DateTime.UtcNow;
        var durations = activeTasks.ToDictionary(
            t => t.Id,
            t =>
            {
                var effectiveEnd = t.Status == PlanTaskStatus.Delayed && t.EndTime < now ? now : t.EndTime;
                var duration = effectiveEnd - t.StartTime;
                return duration.TotalMinutes < 1 ? TimeSpan.FromMinutes(1) : duration;
            });

        var earlyStart = activeTasks.ToDictionary(t => t.Id, t => t.StartTime);
        var earlyFinish = new Dictionary<Guid, DateTime>();

        foreach (var taskId in orderedIds)
        {
            earlyFinish[taskId] = earlyStart[taskId].Add(durations[taskId]);

            foreach (var dependency in outgoing[taskId])
            {
                var successorId = dependency.SuccessorTaskId;
                var candidateStart = dependency.Type switch
                {
                    PlanTaskDependencyType.StartToStart => earlyStart[taskId],
                    PlanTaskDependencyType.FinishToFinish => earlyFinish[taskId].Subtract(durations[successorId]),
                    PlanTaskDependencyType.StartToFinish => earlyStart[taskId].Subtract(durations[successorId]),
                    _ => earlyFinish[taskId]
                };

                if (candidateStart > earlyStart[successorId])
                {
                    earlyStart[successorId] = candidateStart;
                }
            }
        }

        var projectedEnd = earlyFinish.Values.Max();
        var lateFinish = activeTaskIds.ToDictionary(id => id, _ => projectedEnd);
        var lateStart = activeTaskIds.ToDictionary(id => id, id => projectedEnd.Subtract(durations[id]));

        foreach (var taskId in orderedIds.AsEnumerable().Reverse())
        {
            foreach (var dependency in outgoing[taskId])
            {
                var successorId = dependency.SuccessorTaskId;
                var candidateFinish = dependency.Type switch
                {
                    PlanTaskDependencyType.StartToStart => lateStart[successorId].Add(durations[taskId]),
                    PlanTaskDependencyType.FinishToFinish => lateFinish[successorId],
                    PlanTaskDependencyType.StartToFinish => lateFinish[successorId].Add(durations[taskId]),
                    _ => lateStart[successorId]
                };

                if (candidateFinish < lateFinish[taskId])
                {
                    lateFinish[taskId] = candidateFinish;
                    lateStart[taskId] = candidateFinish.Subtract(durations[taskId]);
                }
            }
        }

        var result = new Dictionary<Guid, CriticalPathTaskResult>();
        foreach (var task in activeTasks)
        {
            var slack = (int)Math.Max(0, Math.Round((lateStart[task.Id] - earlyStart[task.Id]).TotalMinutes));
            result[task.Id] = new CriticalPathTaskResult(
                task.Id,
                earlyStart[task.Id],
                earlyFinish[task.Id],
                lateStart[task.Id],
                lateFinish[task.Id],
                slack,
                slack <= 1);
        }

        return new CriticalPathResult(false, null, projectedEnd, result);
    }
}
