using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;
using OmniBizAI.Domain.StateMachines;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;
using OmniBizAI.ViewModels;

namespace OmniBizAI.Services;

public class OperationPlanService
{
    private const string OperationPlanTargetType = "OperationPlan";
    private const string PlanReviewStepCode = "PLAN_REVIEW";

    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly GeminiService _gemini;
    private readonly INumberingService _numbering;
    private readonly IAuditService _audit;
    private readonly CriticalPathCalculator _criticalPath;
    private readonly NotificationService _notifications;
    private readonly ResourceAvailabilityService _availability;

    public OperationPlanService(
        ApplicationDbContext db,
        ITenantContext tenant,
        GeminiService gemini,
        INumberingService numbering,
        IAuditService audit,
        CriticalPathCalculator criticalPath,
        NotificationService notifications,
        ResourceAvailabilityService availability)
    {
        _db = db;
        _tenant = tenant;
        _gemini = gemini;
        _numbering = numbering;
        _audit = audit;
        _criticalPath = criticalPath;
        _notifications = notifications;
        _availability = availability;
    }

    private static int CalculateProgress(IEnumerable<PlanTask> tasks)
    {
        var activeTasks = tasks.Where(t => !t.IsDeleted && t.Status != PlanTaskStatus.Cancelled).ToList();
        return activeTasks.Any() ? (int)activeTasks.Average(t => t.ProgressPercent) : 0;
    }

    private static string BuildGanttTaskClass(PlanTaskViewModel task)
    {
        if (task.IsCriticalPath) return "critical-task";

        return task.Status switch
        {
            "Delayed" => "delayed-task",
            "InProgress" => "in-progress-task",
            "Done" => "done-task",
            "Cancelled" => "cancelled-task",
            _ => "todo-task"
        };
    }

    private async Task<int> EnsurePlanBaselinesAsync(OperationPlan plan, DateTimeOffset snapshotAt)
    {
        var taskIds = plan.Tasks.Where(t => !t.IsDeleted).Select(t => t.Id).ToList();
        if (!taskIds.Any()) return 0;

        var existingTaskIds = await _db.PlanTaskBaselines
            .Where(b => b.TenantId == plan.TenantId && b.PlanId == plan.Id && taskIds.Contains(b.PlanTaskId) && !b.IsDeleted)
            .Select(b => b.PlanTaskId)
            .ToListAsync();

        var missingTasks = plan.Tasks
            .Where(t => !t.IsDeleted && !existingTaskIds.Contains(t.Id))
            .ToList();

        foreach (var task in missingTasks)
        {
            _db.PlanTaskBaselines.Add(new PlanTaskBaseline
            {
                TenantId = plan.TenantId,
                PlanId = plan.Id,
                PlanTaskId = task.Id,
                TaskName = task.Name,
                BaselineStart = task.StartTime,
                BaselineEnd = task.EndTime,
                BaselineAssignedUserId = task.AssignedUserId,
                BaselineEquipmentId = task.EquipmentId,
                SnapshottedAt = snapshotAt,
                SnapshottedByUserId = _tenant.UserId,
                CreatedAt = snapshotAt,
                CreatedByUserId = _tenant.UserId
            });
        }

        if (missingTasks.Any())
        {
            await _audit.LogAsync("OperationPlan", plan.Id, "CreateBaseline",
                newValueObj: new { BaselineTaskCount = missingTasks.Count, SnapshottedAt = snapshotAt });
        }

        return missingTasks.Count;
    }

    private async Task<CriticalPathResult> RecalculateCriticalPathAsync(Guid planId)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == planId && p.TenantId == tid && !p.IsDeleted);

        if (plan == null)
        {
            return new CriticalPathResult(true, "Kế hoạch vận hành không tồn tại.", null, new Dictionary<Guid, CriticalPathTaskResult>());
        }

        var dependencies = await _db.PlanTaskDependencies
            .Where(d => d.TenantId == tid && d.PlanId == planId && !d.IsDeleted)
            .ToListAsync();

        var localDependencyEntries = _db.ChangeTracker.Entries<PlanTaskDependency>()
            .Where(e => e.Entity.TenantId == tid && e.Entity.PlanId == planId)
            .ToList();
        foreach (var entry in localDependencyEntries)
        {
            dependencies.RemoveAll(d => d.Id == entry.Entity.Id);
            if (entry.State != EntityState.Deleted && !entry.Entity.IsDeleted)
            {
                dependencies.Add(entry.Entity);
            }
        }

        var result = _criticalPath.Calculate(plan.Tasks.ToList(), dependencies, DateTime.UtcNow);
        if (result.HasCycle) return result;

        if (plan.ProjectedEndDate != result.ProjectedEndDate)
        {
            plan.ProjectedEndDate = result.ProjectedEndDate;
        }

        foreach (var task in plan.Tasks)
        {
            if (result.Tasks.TryGetValue(task.Id, out var schedule))
            {
                if (task.EarlyStart != schedule.EarlyStart) task.EarlyStart = schedule.EarlyStart;
                if (task.EarlyFinish != schedule.EarlyFinish) task.EarlyFinish = schedule.EarlyFinish;
                if (task.LateStart != schedule.LateStart) task.LateStart = schedule.LateStart;
                if (task.LateFinish != schedule.LateFinish) task.LateFinish = schedule.LateFinish;
                if (task.SlackMinutes != schedule.SlackMinutes) task.SlackMinutes = schedule.SlackMinutes;
                if (task.IsCriticalPath != schedule.IsCritical) task.IsCriticalPath = schedule.IsCritical;
            }
            else
            {
                task.EarlyStart = null;
                task.EarlyFinish = null;
                task.LateStart = null;
                task.LateFinish = null;
                task.SlackMinutes = null;
                task.IsCriticalPath = false;
            }
        }

        return result;
    }

    private async Task<bool> CreatesDependencyCycleAsync(Guid planId, Guid predecessorTaskId, Guid successorTaskId)
    {
        var edges = await _db.PlanTaskDependencies
            .Where(d => d.TenantId == _tenant.TenantId && d.PlanId == planId && !d.IsDeleted)
            .Select(d => new { d.PredecessorTaskId, d.SuccessorTaskId })
            .ToListAsync();

        var outgoing = edges
            .GroupBy(e => e.PredecessorTaskId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.SuccessorTaskId).ToList());

        if (!outgoing.TryGetValue(predecessorTaskId, out var directSuccessors))
        {
            directSuccessors = new List<Guid>();
            outgoing[predecessorTaskId] = directSuccessors;
        }
        directSuccessors.Add(successorTaskId);

        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(successorTaskId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == predecessorTaskId) return true;
            if (!visited.Add(current)) continue;

            if (outgoing.TryGetValue(current, out var nextTasks))
            {
                foreach (var nextTaskId in nextTasks)
                {
                    stack.Push(nextTaskId);
                }
            }
        }

        return false;
    }

    private static string DependencyTypeLabel(PlanTaskDependencyType type) => type switch
    {
        PlanTaskDependencyType.StartToStart => "SS",
        PlanTaskDependencyType.FinishToFinish => "FF",
        PlanTaskDependencyType.StartToFinish => "SF",
        _ => "FS"
    };

    private static decimal Percent(decimal value) => Math.Round(Math.Clamp(value, 0m, 1m) * 100m, 2);

    private static void RecordPlanTaskOee(
        PlanTask task,
        DateTime? actualStartTime,
        DateTime? actualEndTime,
        decimal? unitsProduced,
        decimal? unitsGood)
    {
        var actualStart = actualStartTime ?? task.ActualStartTime ?? task.StartTime;
        var actualEnd = actualEndTime ?? task.ActualEndTime ?? DateTime.UtcNow;
        var plannedMinutes = Math.Max(1, (int)Math.Ceiling((task.EndTime - task.StartTime).TotalMinutes));
        var actualMinutes = Math.Max(1, (int)Math.Ceiling((actualEnd - actualStart).TotalMinutes));

        task.ActualStartTime = actualStart;
        task.ActualEndTime = actualEnd;
        task.PlannedDurationMinutes = plannedMinutes;
        task.ActualDurationMinutes = actualMinutes;

        if (unitsProduced.HasValue) task.UnitsProduced = unitsProduced.Value;
        if (unitsGood.HasValue) task.UnitsGood = unitsGood.Value;

        var availability = Math.Min(1m, actualMinutes / (decimal)plannedMinutes);
        var performance = Math.Min(1m, plannedMinutes / (decimal)actualMinutes);
        var quality = task.UnitsProduced.HasValue && task.UnitsProduced.Value > 0
            ? Math.Min(1m, Math.Max(0m, (task.UnitsGood ?? task.UnitsProduced.Value) / task.UnitsProduced.Value))
            : 1m;

        task.OeeAvailabilityPercent = Percent(availability);
        task.OeePerformancePercent = Percent(performance);
        task.OeeQualityPercent = Percent(quality);
        task.OeePercent = Percent(availability * performance * quality);
    }

    public async Task<List<OperationPlanListViewModel>> GetPlansAsync()
    {
        var tid = _tenant.TenantId;
        var plans = await _db.OperationPlans
            .Include(p => p.Tasks)
            .Where(p => p.TenantId == tid && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return plans.Select(p => {
            var progress = CalculateProgress(p.Tasks);
            return new OperationPlanListViewModel
            {
                Id = p.Id,
                Code = p.Code,
                Title = p.Title,
                PlanType = p.PlanType,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status.ToString(),
                TaskCount = p.Tasks.Count,
                ProgressPercent = progress
            };
        }).ToList();
    }

    public async Task<OperationPlanDetailViewModel?> GetPlanDetailAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.AssignedUser)
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .ThenInclude(t => t.Equipment)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsDeleted);

        if (plan == null) return null;

        var criticalPathResult = await RecalculateCriticalPathAsync(plan.Id);

        if (!criticalPathResult.HasCycle && _db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesWithConcurrencyAsync();
        }

        var progress = CalculateProgress(plan.Tasks);
        var taskIds = plan.Tasks.Select(t => t.Id).ToList();
        var baselines = taskIds.Any()
            ? await _db.PlanTaskBaselines
                .Include(b => b.BaselineAssignedUser)
                .Include(b => b.BaselineEquipment)
                .Where(b => b.TenantId == tid && b.PlanId == plan.Id && taskIds.Contains(b.PlanTaskId) && !b.IsDeleted)
                .ToDictionaryAsync(b => b.PlanTaskId)
            : new Dictionary<Guid, PlanTaskBaseline>();

        var dependencies = await _db.PlanTaskDependencies
            .Include(d => d.PredecessorTask)
            .Include(d => d.SuccessorTask)
            .Where(d => d.TenantId == tid && d.PlanId == plan.Id && !d.IsDeleted)
            .OrderBy(d => d.PredecessorTask!.StartTime)
            .ThenBy(d => d.SuccessorTask!.StartTime)
            .ToListAsync();

        var dependencyItems = dependencies.Select(d => new PlanTaskDependencyViewModel
        {
            Id = d.Id,
            PredecessorTaskId = d.PredecessorTaskId,
            PredecessorTaskName = d.PredecessorTask?.Name ?? "",
            SuccessorTaskId = d.SuccessorTaskId,
            SuccessorTaskName = d.SuccessorTask?.Name ?? "",
            Type = d.Type
        }).ToList();

        var changeOrders = await _db.PlanChangeOrders
            .Include(c => c.PlanTask)
            .Include(c => c.OldAssignedUser)
            .Include(c => c.NewAssignedUser)
            .Include(c => c.OldEquipment)
            .Include(c => c.NewEquipment)
            .Include(c => c.ApprovedByUser)
            .Where(c => c.TenantId == tid && c.PlanId == plan.Id && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(20)
            .ToListAsync();

        var users = await _db.AppUsers
            .Where(u => u.TenantId == tid && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();

        var equipments = await _db.Equipments
            .Where(e => e.TenantId == tid && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .Select(e => new SelectOption { Value = e.Id.ToString(), Text = e.Name })
            .ToListAsync();

        return new OperationPlanDetailViewModel
        {
            Id = plan.Id,
            Code = plan.Code,
            Title = plan.Title,
            PlanType = plan.PlanType,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            Status = plan.Status.ToString(),
            Notes = plan.Notes,
            ProgressPercent = progress,
            ProjectedEndDate = plan.ProjectedEndDate,
            CriticalPathError = criticalPathResult.HasCycle ? criticalPathResult.Error : null,
            NextStatuses = OperationPlanStateMachine.NextStates(plan.Status).Select(s => s.ToString()).ToList(),
            Tasks = plan.Tasks.OrderBy(t => t.StartTime).Select(t => new PlanTaskViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                AssignedUserId = t.AssignedUserId,
                AssignedUserName = t.AssignedUser?.FullName,
                EquipmentId = t.EquipmentId,
                EquipmentName = t.Equipment?.Name,
                Status = t.Status.ToString(),
                NextStatuses = PlanTaskStateMachine.NextStates(t.Status).Select(s => s.ToString()).ToList(),
                ProgressPercent = t.ProgressPercent,
                EarlyStart = t.EarlyStart,
                EarlyFinish = t.EarlyFinish,
                LateStart = t.LateStart,
                LateFinish = t.LateFinish,
                SlackMinutes = t.SlackMinutes,
                IsCriticalPath = t.IsCriticalPath,
                ActualStartTime = t.ActualStartTime,
                ActualEndTime = t.ActualEndTime,
                PlannedDurationMinutes = t.PlannedDurationMinutes,
                ActualDurationMinutes = t.ActualDurationMinutes,
                UnitsProduced = t.UnitsProduced,
                UnitsGood = t.UnitsGood,
                OeeAvailabilityPercent = t.OeeAvailabilityPercent,
                OeePerformancePercent = t.OeePerformancePercent,
                OeeQualityPercent = t.OeeQualityPercent,
                OeePercent = t.OeePercent,
                Baseline = baselines.TryGetValue(t.Id, out var baseline)
                    ? new PlanTaskBaselineViewModel
                    {
                        BaselineStart = baseline.BaselineStart,
                        BaselineEnd = baseline.BaselineEnd,
                        BaselineAssignedUserName = baseline.BaselineAssignedUser?.FullName,
                        BaselineEquipmentName = baseline.BaselineEquipment?.Name
                    }
                    : null,
                Predecessors = dependencyItems.Where(d => d.SuccessorTaskId == t.Id).ToList(),
                Successors = dependencyItems.Where(d => d.PredecessorTaskId == t.Id).ToList()
            }).ToList(),
            Dependencies = dependencyItems,
            ChangeOrders = changeOrders.Select(c => new PlanChangeOrderViewModel
            {
                Id = c.Id,
                TaskName = c.PlanTask?.Name ?? "",
                OldStartTime = c.OldStartTime,
                NewStartTime = c.NewStartTime,
                OldEndTime = c.OldEndTime,
                NewEndTime = c.NewEndTime,
                OldAssignedUserName = c.OldAssignedUser?.FullName,
                NewAssignedUserName = c.NewAssignedUser?.FullName,
                OldEquipmentName = c.OldEquipment?.Name,
                NewEquipmentName = c.NewEquipment?.Name,
                Reason = c.Reason,
                Status = c.Status.ToString(),
                ApprovedByName = c.ApprovedByUser?.FullName,
                ApprovedAt = c.ApprovedAt,
                CreatedAt = c.CreatedAt
            }).ToList(),
            DependencyTaskOptions = plan.Tasks
                .OrderBy(t => t.StartTime)
                .Select(t => new SelectOption { Value = t.Id.ToString(), Text = t.Name })
                .ToList(),
            Users = users,
            Equipments = equipments
        };
    }

    public async Task<OperationPlanGanttViewModel?> GetPlanGanttAsync(Guid id)
    {
        var plan = await GetPlanDetailAsync(id);
        if (plan == null) return null;

        var predecessorMap = plan.Dependencies
            .GroupBy(d => d.SuccessorTaskId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(d => d.PredecessorTaskId).Distinct().ToList());

        return new OperationPlanGanttViewModel
        {
            Id = plan.Id,
            Code = plan.Code,
            Title = plan.Title,
            PlanType = plan.PlanType,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            ProjectedEndDate = plan.ProjectedEndDate,
            Status = plan.Status,
            ProgressPercent = plan.ProgressPercent,
            Tasks = plan.Tasks.Select(t => new PlanGanttTaskViewModel
            {
                Id = t.Id,
                Name = t.Name,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                Status = t.Status,
                ProgressPercent = t.ProgressPercent,
                IsCriticalPath = t.IsCriticalPath,
                SlackMinutes = t.SlackMinutes,
                Dependencies = predecessorMap.TryGetValue(t.Id, out var dependencies)
                    ? dependencies
                    : new List<Guid>(),
                CssClass = BuildGanttTaskClass(t)
            }).ToList()
        };
    }

    public async Task<int> ReconcileDelayedTasksAsync(CancellationToken cancellationToken = default)
    {
        var tid = _tenant.TenantId;
        var now = DateTime.UtcNow;
        var candidatePlanIds = await _db.OperationPlans
            .Where(p => p.TenantId == tid
                && !p.IsDeleted
                && p.Status == OperationPlanStatus.InProgress
                && p.Tasks.Any(t => !t.IsDeleted
                    && t.EndTime < now
                    && t.Status != PlanTaskStatus.Done
                    && t.Status != PlanTaskStatus.Delayed
                    && t.Status != PlanTaskStatus.Cancelled))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var markedCount = 0;
        foreach (var planId in candidatePlanIds)
        {
            var plan = await _db.OperationPlans
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == planId && p.TenantId == tid && !p.IsDeleted, cancellationToken);
            if (plan == null) continue;

            await RecalculateCriticalPathAsync(plan.Id);
            var criticalDelayedTasks = new List<string>();

            foreach (var task in plan.Tasks.Where(t => t.EndTime < now && PlanTaskStateMachine.CanTransition(t.Status, PlanTaskStatus.Delayed)))
            {
                var oldStatus = task.Status;
                task.Status = PlanTaskStatus.Delayed;
                task.UpdatedAt = DateTimeOffset.UtcNow;
                task.UpdatedByUserId = _tenant.UserId;
                markedCount++;

                if (task.IsCriticalPath)
                {
                    criticalDelayedTasks.Add(task.Name);
                }

                await _audit.LogAsync("PlanTask", task.Id, "MarkDelayed",
                    oldValueObj: new { Status = oldStatus },
                    newValueObj: new { task.Status },
                    extra: new { task.PlanId, task.EndTime, task.IsCriticalPath });
            }

            if (!criticalDelayedTasks.Any() && !_db.ChangeTracker.HasChanges()) continue;

            var criticalPathResult = await RecalculateCriticalPathAsync(plan.Id);
            await _db.SaveChangesWithConcurrencyAsync();

            if (criticalDelayedTasks.Any())
            {
                await _notifications.SendToManagersAsync(
                    $"Critical path bị trễ trong kế hoạch {plan.Code}",
                    $"Các công việc critical path bị trễ: {string.Join(", ", criticalDelayedTasks)}. Projected end: {criticalPathResult.ProjectedEndDate:dd/MM/yyyy HH:mm}.",
                    "OperationPlan",
                    plan.Id);
            }
        }

        return markedCount;
    }

    public async Task<Guid> CreatePlanAsync(OperationPlanCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var code = await _numbering.NextAsync(NumberingSequenceKeys.OperationPlan, "OPP-", 4);
        var plan = new OperationPlan
        {
            TenantId = tid,
            Code = code,
            Title = vm.Title,
            PlanType = vm.PlanType,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            Notes = vm.Notes,
            Status = OperationPlanStatus.Draft,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.OperationPlans.Add(plan);
        await _audit.LogAsync("OperationPlan", plan.Id, "Create",
            newValueObj: new { plan.Code, plan.Title, plan.PlanType, plan.StartDate, plan.EndDate, plan.Status });
        await _db.SaveChangesAsync();
        return plan.Id;
    }

    public async Task<PlanTaskCreateViewModel> GetCreateTaskFormAsync(Guid planId)
    {
        var tid = _tenant.TenantId;
        var users = await _db.AppUsers.Where(u => u.TenantId == tid && !u.IsDeleted)
            .Select(u => new SelectOption { Value = u.Id.ToString(), Text = u.FullName })
            .ToListAsync();
            
        var equipments = await _db.Equipments.Where(e => e.TenantId == tid && !e.IsDeleted)
            .Select(e => new SelectOption { Value = e.Id.ToString(), Text = e.Name })
            .ToListAsync();

        return new PlanTaskCreateViewModel
        {
            PlanId = planId,
            StartTime = DateTime.Today,
            EndTime = DateTime.Today.AddDays(1),
            Users = users,
            Equipments = equipments
        };
    }

    public async Task<(bool Success, string Message)> CreateTaskAsync(PlanTaskCreateViewModel vm)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .FirstOrDefaultAsync(p => p.Id == vm.PlanId && p.TenantId == tid && !p.IsDeleted);
        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (plan.Status != OperationPlanStatus.Draft)
        {
            return (false, "Chỉ được thêm hoặc thay đổi công việc khi kế hoạch còn ở trạng thái nháp.");
        }

        if (vm.EndTime <= vm.StartTime)
        {
            return (false, "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }

        var availability = await _availability.CheckPlanTaskBookingAsync(
            vm.AssignedUserId,
            vm.EquipmentId,
            vm.StartTime,
            vm.EndTime);
        if (!availability.CanBook)
            return (false, availability.BlockMessage());

        var task = new PlanTask
        {
            TenantId = tid,
            PlanId = vm.PlanId,
            Name = vm.Name,
            Description = vm.Description,
            StartTime = vm.StartTime,
            EndTime = vm.EndTime,
            AssignedUserId = vm.AssignedUserId,
            EquipmentId = vm.EquipmentId,
            Status = PlanTaskStatus.Todo,
            ProgressPercent = 0,
            CreatedByUserId = _tenant.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.PlanTasks.Add(task);

        await _audit.LogAsync("PlanTask", task.Id, "Create",
            newValueObj: new { task.PlanId, task.Name, task.StartTime, task.EndTime, task.AssignedUserId, task.EquipmentId, task.Status });

        return await _db.SaveChangesWithConcurrencyMessageAsync($"Đã phân công công việc mới.{availability.WarningSuffix()}");
    }

    public async Task<(bool Success, string Message)> UpdateTaskStatusAsync(
        Guid planId,
        Guid taskId,
        PlanTaskStatus newStatus,
        int progressPercent,
        DateTime? actualStartTime = null,
        DateTime? actualEndTime = null,
        decimal? unitsProduced = null,
        decimal? unitsGood = null)
    {
        var tid = _tenant.TenantId;
        var task = await _db.PlanTasks
            .Include(t => t.Plan)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.PlanId == planId && t.TenantId == tid && !t.IsDeleted);

        if (task?.Plan == null || task.Plan.TenantId != tid || task.Plan.IsDeleted)
            return (false, "Công việc hoặc kế hoạch không tồn tại.");

        if (task.Plan.Status is not (OperationPlanStatus.Approved or OperationPlanStatus.InProgress))
            return (false, "Chỉ được cập nhật trạng thái/tiến độ công việc sau khi kế hoạch đã được duyệt.");

        if (progressPercent is < 0 or > 100)
            return (false, "Tiến độ công việc phải nằm trong khoảng 0-100%.");

        if (unitsProduced is < 0 || unitsGood is < 0)
            return (false, "Sản lượng không được nhỏ hơn 0.");

        if (unitsProduced.HasValue && unitsGood.HasValue && unitsGood.Value > unitsProduced.Value)
            return (false, "Sản lượng đạt chuẩn không được lớn hơn tổng sản lượng.");

        if (newStatus == PlanTaskStatus.Done)
        {
            var actualStart = actualStartTime ?? task.ActualStartTime ?? task.StartTime;
            var actualEnd = actualEndTime ?? task.ActualEndTime ?? DateTime.UtcNow;
            if (actualEnd <= actualStart)
                return (false, "Actual end phải lớn hơn actual start để tính OEE.");
        }

        if (task.Status != newStatus && !PlanTaskStateMachine.CanTransition(task.Status, newStatus))
            return (false, "Trạng thái công việc hiện tại không cho phép chuyển đổi này.");

        var oldStatus = task.Status;
        var oldProgress = task.ProgressPercent;

        task.Status = newStatus;
        task.ProgressPercent = newStatus switch
        {
            PlanTaskStatus.Todo => 0,
            PlanTaskStatus.Done => 100,
            PlanTaskStatus.Cancelled => task.ProgressPercent,
            _ => progressPercent
        };

        if (newStatus == PlanTaskStatus.InProgress && !task.ActualStartTime.HasValue)
        {
            task.ActualStartTime = actualStartTime ?? DateTime.UtcNow;
        }

        if (newStatus == PlanTaskStatus.Done)
        {
            RecordPlanTaskOee(task, actualStartTime, actualEndTime, unitsProduced, unitsGood);
        }
        else
        {
            if (actualStartTime.HasValue) task.ActualStartTime = actualStartTime;
            if (actualEndTime.HasValue) task.ActualEndTime = actualEndTime;
            if (unitsProduced.HasValue) task.UnitsProduced = unitsProduced.Value;
            if (unitsGood.HasValue) task.UnitsGood = unitsGood.Value;
        }

        task.UpdatedAt = DateTimeOffset.UtcNow;
        task.UpdatedByUserId = _tenant.UserId;

        await _audit.LogAsync("PlanTask", task.Id, "UpdateProgress",
            oldValueObj: new { Status = oldStatus, ProgressPercent = oldProgress },
            newValueObj: new
            {
                task.Status,
                task.ProgressPercent,
                task.ActualStartTime,
                task.ActualEndTime,
                task.PlannedDurationMinutes,
                task.ActualDurationMinutes,
                task.UnitsProduced,
                task.UnitsGood,
                task.OeePercent
            },
            extra: new { task.PlanId });

        await RecalculateCriticalPathAsync(planId);
        return await _db.SaveChangesWithConcurrencyMessageAsync("Đã cập nhật tiến độ công việc.");
    }

    public async Task<(bool Success, string Message)> ApplyTaskChangeOrderAsync(
        Guid planId,
        Guid taskId,
        DateTime newStartTime,
        DateTime newEndTime,
        Guid? newAssignedUserId,
        Guid? newEquipmentId,
        string reason)
    {
        var tid = _tenant.TenantId;
        var task = await _db.PlanTasks
            .Include(t => t.Plan)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.PlanId == planId && t.TenantId == tid && !t.IsDeleted);

        if (task?.Plan == null || task.Plan.TenantId != tid || task.Plan.IsDeleted)
            return (false, "Công việc hoặc kế hoạch không tồn tại.");

        if (task.Plan.Status is not (OperationPlanStatus.Approved or OperationPlanStatus.InProgress))
            return (false, "Chỉ được lập change order sau khi kế hoạch đã được duyệt.");

        if (task.Status == PlanTaskStatus.Cancelled)
            return (false, "Không thể thay đổi công việc đã hủy.");

        if (newEndTime <= newStartTime)
            return (false, "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            return (false, "Vui lòng nhập lý do thay đổi tối thiểu 10 ký tự.");

        var hasChange = task.StartTime != newStartTime
            || task.EndTime != newEndTime
            || task.AssignedUserId != newAssignedUserId
            || task.EquipmentId != newEquipmentId;
        if (!hasChange)
            return (false, "Không có thay đổi nào để lập change order.");

        var availability = await _availability.CheckPlanTaskBookingAsync(
            newAssignedUserId,
            newEquipmentId,
            newStartTime,
            newEndTime,
            task.Id);
        if (!availability.CanBook)
            return (false, availability.BlockMessage());

        var changeAt = DateTimeOffset.UtcNow;
        var changeOrder = new PlanChangeOrder
        {
            TenantId = tid,
            PlanId = task.PlanId,
            PlanTaskId = task.Id,
            OldStartTime = task.StartTime,
            NewStartTime = newStartTime,
            OldEndTime = task.EndTime,
            NewEndTime = newEndTime,
            OldAssignedUserId = task.AssignedUserId,
            NewAssignedUserId = newAssignedUserId,
            OldEquipmentId = task.EquipmentId,
            NewEquipmentId = newEquipmentId,
            Reason = reason.Trim(),
            Status = PlanChangeOrderStatus.Approved,
            ApprovedByUserId = _tenant.UserId,
            ApprovedAt = changeAt,
            CreatedAt = changeAt,
            CreatedByUserId = _tenant.UserId
        };
        _db.PlanChangeOrders.Add(changeOrder);

        task.StartTime = newStartTime;
        task.EndTime = newEndTime;
        task.AssignedUserId = newAssignedUserId;
        task.EquipmentId = newEquipmentId;
        task.UpdatedAt = changeAt;
        task.UpdatedByUserId = _tenant.UserId;
        await RecalculateCriticalPathAsync(planId);

        await _audit.LogAsync("PlanTask", task.Id, "ChangeOrder",
            oldValueObj: new
            {
                changeOrder.OldStartTime,
                changeOrder.OldEndTime,
                changeOrder.OldAssignedUserId,
                changeOrder.OldEquipmentId
            },
            newValueObj: new
            {
                changeOrder.NewStartTime,
                changeOrder.NewEndTime,
                changeOrder.NewAssignedUserId,
                changeOrder.NewEquipmentId
            },
            extra: new { ChangeOrderId = changeOrder.Id, changeOrder.Reason, changeOrder.Status });

        return await _db.SaveChangesWithConcurrencyMessageAsync($"Đã áp dụng change order cho công việc.{availability.WarningSuffix()}");
    }

    public async Task<(bool Success, string Message)> UpdateTaskScheduleFromGanttAsync(
        Guid planId,
        Guid taskId,
        DateTime startTime,
        DateTime endTime)
    {
        var tid = _tenant.TenantId;
        var task = await _db.PlanTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId && t.PlanId == planId && t.TenantId == tid && !t.IsDeleted);

        if (task == null)
            return (false, "Công việc hoặc kế hoạch không tồn tại.");

        return await ApplyTaskChangeOrderAsync(
            planId,
            taskId,
            startTime,
            endTime,
            task.AssignedUserId,
            task.EquipmentId,
            "Điều chỉnh lịch từ Gantt view.");
    }

    public async Task<(bool Success, string Message)> AddTaskDependencyAsync(
        Guid planId,
        Guid predecessorTaskId,
        Guid successorTaskId,
        PlanTaskDependencyType type)
    {
        var tid = _tenant.TenantId;
        if (predecessorTaskId == successorTaskId)
            return (false, "Một công việc không thể phụ thuộc vào chính nó.");

        var plan = await _db.OperationPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.TenantId == tid && !p.IsDeleted);
        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (plan.Status is OperationPlanStatus.Completed or OperationPlanStatus.Cancelled)
            return (false, "Không thể thay đổi dependency khi kế hoạch đã hoàn thành hoặc đã hủy.");

        var tasks = await _db.PlanTasks
            .Where(t => t.TenantId == tid
                && t.PlanId == planId
                && !t.IsDeleted
                && (t.Id == predecessorTaskId || t.Id == successorTaskId))
            .Select(t => t.Id)
            .ToListAsync();
        if (tasks.Count != 2)
            return (false, "Công việc predecessor hoặc successor không thuộc kế hoạch này.");

        var exists = await _db.PlanTaskDependencies.AnyAsync(d =>
            d.TenantId == tid
            && d.PlanId == planId
            && !d.IsDeleted
            && d.PredecessorTaskId == predecessorTaskId
            && d.SuccessorTaskId == successorTaskId
            && d.Type == type);
        if (exists)
            return (false, "Dependency này đã tồn tại.");

        if (await CreatesDependencyCycleAsync(planId, predecessorTaskId, successorTaskId))
            return (false, "Dependency này tạo vòng lặp giữa các công việc, vui lòng chọn quan hệ khác.");

        var dependency = new PlanTaskDependency
        {
            TenantId = tid,
            PlanId = planId,
            PredecessorTaskId = predecessorTaskId,
            SuccessorTaskId = successorTaskId,
            Type = type,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = _tenant.UserId
        };
        _db.PlanTaskDependencies.Add(dependency);

        var criticalPathResult = await RecalculateCriticalPathAsync(planId);
        if (criticalPathResult.HasCycle)
            return (false, criticalPathResult.Error ?? "Dependency tạo vòng lặp, không thể tính critical path.");

        await _audit.LogAsync("PlanTaskDependency", dependency.Id, "Create",
            newValueObj: new { dependency.PlanId, dependency.PredecessorTaskId, dependency.SuccessorTaskId, Type = DependencyTypeLabel(type) },
            extra: new { criticalPathResult.ProjectedEndDate });

        return await _db.SaveChangesWithConcurrencyMessageAsync("Đã thêm dependency và tính lại critical path.");
    }

    public async Task<(bool Success, string Message)> DeleteTaskDependencyAsync(Guid planId, Guid dependencyId)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans.FirstOrDefaultAsync(p => p.Id == planId && p.TenantId == tid && !p.IsDeleted);
        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (plan.Status is OperationPlanStatus.Completed or OperationPlanStatus.Cancelled)
            return (false, "Không thể thay đổi dependency khi kế hoạch đã hoàn thành hoặc đã hủy.");

        var dependency = await _db.PlanTaskDependencies
            .FirstOrDefaultAsync(d => d.Id == dependencyId && d.PlanId == planId && d.TenantId == tid && !d.IsDeleted);
        if (dependency == null)
            return (false, "Dependency không tồn tại.");

        dependency.IsDeleted = true;
        dependency.UpdatedAt = DateTimeOffset.UtcNow;
        dependency.UpdatedByUserId = _tenant.UserId;

        var criticalPathResult = await RecalculateCriticalPathAsync(planId);
        await _audit.LogAsync("PlanTaskDependency", dependency.Id, "Delete",
            oldValueObj: new { dependency.PlanId, dependency.PredecessorTaskId, dependency.SuccessorTaskId, Type = DependencyTypeLabel(dependency.Type) },
            extra: new { criticalPathResult.ProjectedEndDate });

        return await _db.SaveChangesWithConcurrencyMessageAsync("Đã xóa dependency và tính lại critical path.");
    }

    public async Task<(bool Success, string Message)> SubmitPlanAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsDeleted);

        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.Submitted))
            return (false, "Trạng thái hiện tại không cho phép gửi duyệt kế hoạch.");

        if (!plan.Tasks.Any(t => !t.IsDeleted))
            return (false, "Kế hoạch cần có ít nhất một công việc trước khi gửi duyệt.");

        if (plan.EndDate <= plan.StartDate)
            return (false, "Thời gian kết thúc kế hoạch phải lớn hơn thời gian bắt đầu.");

        var hasPendingApproval = await _db.ApprovalTasks.AnyAsync(t =>
            t.TenantId == tid
            && !t.IsDeleted
            && t.TargetType == OperationPlanTargetType
            && t.TargetId == plan.Id
            && t.Status == ApprovalStatus.Pending);

        var oldStatus = plan.Status;
        var submittedAt = DateTimeOffset.UtcNow;
        plan.Status = OperationPlanStatus.Submitted;
        plan.UpdatedAt = submittedAt;
        plan.UpdatedByUserId = _tenant.UserId;

        if (!hasPendingApproval)
        {
            var approvalTask = new ApprovalTask
            {
                TenantId = tid,
                TargetType = OperationPlanTargetType,
                TargetId = plan.Id,
                StepCode = PlanReviewStepCode,
                AssignedRole = "EXECUTIVE",
                Status = ApprovalStatus.Pending,
                CreatedAt = submittedAt,
                CreatedByUserId = _tenant.UserId
            };
            _db.ApprovalTasks.Add(approvalTask);

            await _audit.LogAsync("ApprovalTask", approvalTask.Id, "Create",
                newValueObj: new { approvalTask.TargetType, approvalTask.TargetId, approvalTask.StepCode, approvalTask.AssignedRole, approvalTask.Status });
        }

        await _audit.LogAsync("OperationPlan", plan.Id, "Submit",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { plan.Status },
            extra: new { ApprovalStep = PlanReviewStepCode, AssignedRole = "EXECUTIVE" });

        return await _db.SaveChangesWithConcurrencyMessageAsync("Kế hoạch đã được gửi duyệt.");
    }

    public async Task<(bool Success, string Message)> ApprovePlanAsync(Guid id, string? note = null)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsDeleted);
        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.Approved))
            return (false, "Trạng thái hiện tại không cho phép phê duyệt kế hoạch.");

        var oldStatus = plan.Status;
        var decidedAt = DateTimeOffset.UtcNow;
        plan.Status = OperationPlanStatus.Approved;
        plan.UpdatedAt = decidedAt;
        plan.UpdatedByUserId = _tenant.UserId;
        var baselineCount = await EnsurePlanBaselinesAsync(plan, decidedAt);
        var criticalPathResult = await RecalculateCriticalPathAsync(plan.Id);

        var pendingTasks = await _db.ApprovalTasks
            .Where(t => t.TenantId == tid
                && !t.IsDeleted
                && t.TargetType == OperationPlanTargetType
                && t.TargetId == plan.Id
                && t.Status == ApprovalStatus.Pending)
            .ToListAsync();

        foreach (var task in pendingTasks)
        {
            task.Status = ApprovalStatus.Approved;
            task.DecisionNote = note;
            task.DecidedAt = decidedAt;
            task.UpdatedAt = decidedAt;
            task.UpdatedByUserId = _tenant.UserId;

            await _audit.LogAsync("ApprovalTask", task.Id, "Approve",
                oldValueObj: new { Status = ApprovalStatus.Pending },
                newValueObj: new { task.Status, task.TargetType, task.TargetId, task.StepCode },
                extra: new { Note = note });
        }

        await _audit.LogAsync("OperationPlan", plan.Id, "Approve",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { plan.Status },
            extra: new { ApprovalTaskCount = pendingTasks.Count, BaselineTaskCount = baselineCount, criticalPathResult.ProjectedEndDate, Note = note });

        return await _db.SaveChangesWithConcurrencyMessageAsync("Kế hoạch đã được phê duyệt.");
    }

    public async Task<(bool Success, string Message)> StartPlanAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsDeleted);
        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.InProgress))
            return (false, "Chỉ kế hoạch đã duyệt mới được bắt đầu thực hiện.");

        var oldStatus = plan.Status;
        plan.Status = OperationPlanStatus.InProgress;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        plan.UpdatedByUserId = _tenant.UserId;

        await _audit.LogAsync("OperationPlan", plan.Id, "Start",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { plan.Status });

        return await _db.SaveChangesWithConcurrencyMessageAsync("Kế hoạch đã chuyển sang đang thực hiện.");
    }

    public async Task<(bool Success, string Message)> CompletePlanAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsDeleted);

        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.Completed))
            return (false, "Chỉ kế hoạch đang thực hiện mới được hoàn thành.");

        var activeTasks = plan.Tasks.Where(t => !t.IsDeleted && t.Status != PlanTaskStatus.Cancelled).ToList();
        if (!activeTasks.Any())
            return (false, "Kế hoạch cần có công việc hợp lệ trước khi hoàn thành.");

        if (activeTasks.Any(t => t.Status != PlanTaskStatus.Done))
            return (false, "Chỉ được hoàn thành kế hoạch khi tất cả công việc đã xong hoặc đã hủy.");

        var oldStatus = plan.Status;
        plan.Status = OperationPlanStatus.Completed;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        plan.UpdatedByUserId = _tenant.UserId;

        await _audit.LogAsync("OperationPlan", plan.Id, "Complete",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { plan.Status, ProgressPercent = CalculateProgress(plan.Tasks) });

        return await _db.SaveChangesWithConcurrencyMessageAsync("Kế hoạch đã hoàn thành.");
    }

    public async Task<(bool Success, string Message)> CancelPlanAsync(Guid id)
    {
        var tid = _tenant.TenantId;
        var plan = await _db.OperationPlans
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tid && !p.IsDeleted);

        if (plan == null)
            return (false, "Kế hoạch vận hành không tồn tại hoặc bạn không có quyền truy cập.");

        if (!OperationPlanStateMachine.CanTransition(plan.Status, OperationPlanStatus.Cancelled))
            return (false, "Trạng thái hiện tại không cho phép hủy kế hoạch.");

        var oldStatus = plan.Status;
        var cancelledAt = DateTimeOffset.UtcNow;
        plan.Status = OperationPlanStatus.Cancelled;
        plan.UpdatedAt = cancelledAt;
        plan.UpdatedByUserId = _tenant.UserId;

        var cancelledTaskCount = 0;
        foreach (var task in plan.Tasks.Where(t => PlanTaskStateMachine.CanTransition(t.Status, PlanTaskStatus.Cancelled)))
        {
            task.Status = PlanTaskStatus.Cancelled;
            task.UpdatedAt = cancelledAt;
            task.UpdatedByUserId = _tenant.UserId;
            cancelledTaskCount++;
        }

        var pendingApprovalTasks = await _db.ApprovalTasks
            .Where(t => t.TenantId == tid
                && !t.IsDeleted
                && t.TargetType == OperationPlanTargetType
                && t.TargetId == plan.Id
                && t.Status == ApprovalStatus.Pending)
            .ToListAsync();

        foreach (var approvalTask in pendingApprovalTasks)
        {
            approvalTask.Status = ApprovalStatus.Cancelled;
            approvalTask.DecisionNote = "Kế hoạch đã bị hủy.";
            approvalTask.DecidedAt = cancelledAt;
            approvalTask.UpdatedAt = cancelledAt;
            approvalTask.UpdatedByUserId = _tenant.UserId;

            await _audit.LogAsync("ApprovalTask", approvalTask.Id, "Cancel",
                oldValueObj: new { Status = ApprovalStatus.Pending },
                newValueObj: new { approvalTask.Status, approvalTask.TargetType, approvalTask.TargetId, approvalTask.StepCode });
        }

        await _audit.LogAsync("OperationPlan", plan.Id, "Cancel",
            oldValueObj: new { Status = oldStatus },
            newValueObj: new { plan.Status },
            extra: new { CancelledTaskCount = cancelledTaskCount, CancelledApprovalTaskCount = pendingApprovalTasks.Count });

        return await _db.SaveChangesWithConcurrencyMessageAsync("Kế hoạch đã được hủy.");
    }

    public async Task<string> AnalyzePlanWithAiAsync(Guid planId)
    {
        var plan = await GetPlanDetailAsync(planId);
        if (plan == null) return "Plan not found.";

        var prompt = $"Phân tích Kế hoạch vận hành/sản xuất '{plan.Title}' (Từ {plan.StartDate:d} đến {plan.EndDate:d}).\n" +
                     $"Tiến độ hiện tại: {plan.ProgressPercent}%.\n" +
                     $"Các công việc:\n";
        
        foreach(var t in plan.Tasks) {
            prompt += $"- {t.Name}: Hạn {t.EndTime:g}, Trạng thái: {t.Status}, Phụ trách: {t.AssignedUserName ?? "Trống"}, Thiết bị: {t.EquipmentName ?? "Trống"}\n";
        }

        prompt += "\nHãy chỉ ra rủi ro (đặc biệt là các công việc bị 'Delayed') và đưa ra đề xuất điều chỉnh lịch trình ngắn gọn.";

        var response = await _gemini.GenerateAsync(
            "Bạn là trợ lý AI chuyên nghiệp phân tích Kế hoạch Vận hành và Quản lý Rủi ro.", 
            prompt);
            
        return response.Success ? response.Text : response.ErrorMessage ?? "Lỗi khi gọi AI.";
    }
}
