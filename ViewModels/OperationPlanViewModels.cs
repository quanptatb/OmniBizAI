using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.ViewModels;

public class OperationPlanListViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public int TaskCount { get; set; }

    public string StatusLabel => Status switch
    {
        "Draft" => "Nháp",
        "Submitted" => "Chờ duyệt",
        "Approved" => "Đã duyệt",
        "InProgress" => "Đang thực hiện",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => Status
    };

    public string StatusBadgeClass => Status switch
    {
        "Draft" => "bg-secondary",
        "Submitted" => "bg-warning text-dark",
        "Approved" => "bg-info",
        "InProgress" => "bg-primary",
        "Completed" => "bg-success",
        "Cancelled" => "bg-dark",
        _ => "bg-secondary"
    };
}

public class OperationPlanDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime? ProjectedEndDate { get; set; }
    public int CriticalTaskCount => Tasks.Count(t => t.IsCriticalPath);
    public string? CriticalPathError { get; set; }
    public List<string> NextStatuses { get; set; } = new();
    public List<PlanTaskViewModel> Tasks { get; set; } = new();
    public List<PlanChangeOrderViewModel> ChangeOrders { get; set; } = new();
    public List<PlanTaskDependencyViewModel> Dependencies { get; set; } = new();
    public List<SelectOption> DependencyTaskOptions { get; set; } = new();
    public List<SelectOption> Users { get; set; } = new();
    public List<SelectOption> Equipments { get; set; } = new();

    public bool CanAddTasks => Status == "Draft";
    public bool CanRequestChangeOrder => Status is "Approved" or "InProgress";
    public bool CanSubmit => NextStatuses.Contains("Submitted");
    public bool CanStart => NextStatuses.Contains("InProgress");
    public bool CanComplete => NextStatuses.Contains("Completed");
    public bool CanCancel => NextStatuses.Contains("Cancelled");

    public string StatusLabel => Status switch
    {
        "Draft" => "Nháp",
        "Submitted" => "Chờ duyệt",
        "Approved" => "Đã duyệt",
        "InProgress" => "Đang thực hiện",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => Status
    };

    public string StatusBadgeClass => Status switch
    {
        "Draft" => "bg-secondary",
        "Submitted" => "bg-warning text-dark",
        "Approved" => "bg-info",
        "InProgress" => "bg-primary",
        "Completed" => "bg-success",
        "Cancelled" => "bg-dark",
        _ => "bg-secondary"
    };
}

public class OperationPlanGanttViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ProjectedEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public bool CanEditSchedule => Status is "Approved" or "InProgress";
    public List<PlanGanttTaskViewModel> Tasks { get; set; } = new();

    public string StatusLabel => Status switch
    {
        "Draft" => "Nháp",
        "Submitted" => "Chờ duyệt",
        "Approved" => "Đã duyệt",
        "InProgress" => "Đang thực hiện",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => Status
    };

    public string StatusBadgeClass => Status switch
    {
        "Draft" => "bg-secondary",
        "Submitted" => "bg-warning text-dark",
        "Approved" => "bg-info",
        "InProgress" => "bg-primary",
        "Completed" => "bg-success",
        "Cancelled" => "bg-dark",
        _ => "bg-secondary"
    };
}

public class PlanGanttTaskViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public bool IsCriticalPath { get; set; }
    public int? SlackMinutes { get; set; }
    public List<Guid> Dependencies { get; set; } = new();
    public string CssClass { get; set; } = string.Empty;
}

public class PlanTaskViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public DateTime? EarlyStart { get; set; }
    public DateTime? EarlyFinish { get; set; }
    public DateTime? LateStart { get; set; }
    public DateTime? LateFinish { get; set; }
    public int? SlackMinutes { get; set; }
    public bool IsCriticalPath { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public int? PlannedDurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public decimal? UnitsProduced { get; set; }
    public decimal? UnitsGood { get; set; }
    public decimal? OeeAvailabilityPercent { get; set; }
    public decimal? OeePerformancePercent { get; set; }
    public decimal? OeeQualityPercent { get; set; }
    public decimal? OeePercent { get; set; }
    public List<string> NextStatuses { get; set; } = new();
    public PlanTaskBaselineViewModel? Baseline { get; set; }
    public List<PlanTaskDependencyViewModel> Predecessors { get; set; } = new();
    public List<PlanTaskDependencyViewModel> Successors { get; set; } = new();

    public string SlackLabel => SlackMinutes.HasValue
        ? SlackMinutes.Value < 60 ? $"{SlackMinutes.Value} phút" : $"{SlackMinutes.Value / 60.0:0.#} giờ"
        : "N/A";

    public string OeeLabel => OeePercent.HasValue ? $"{OeePercent.Value:0.#}%" : "Chưa có";

    public string StatusLabel => Status switch
    {
        "Todo" => "Cần làm",
        "InProgress" => "Đang làm",
        "Done" => "Xong",
        "Delayed" => "Trễ hạn",
        "Cancelled" => "Đã hủy",
        _ => Status
    };
}

public class PlanTaskDependencyViewModel
{
    public Guid Id { get; set; }
    public Guid PredecessorTaskId { get; set; }
    public string PredecessorTaskName { get; set; } = string.Empty;
    public Guid SuccessorTaskId { get; set; }
    public string SuccessorTaskName { get; set; } = string.Empty;
    public PlanTaskDependencyType Type { get; set; }

    public string TypeLabel => Type switch
    {
        PlanTaskDependencyType.StartToStart => "SS",
        PlanTaskDependencyType.FinishToFinish => "FF",
        PlanTaskDependencyType.StartToFinish => "SF",
        _ => "FS"
    };

    public string TypeDescription => Type switch
    {
        PlanTaskDependencyType.StartToStart => "Start-to-Start",
        PlanTaskDependencyType.FinishToFinish => "Finish-to-Finish",
        PlanTaskDependencyType.StartToFinish => "Start-to-Finish",
        _ => "Finish-to-Start"
    };
}

public class PlanTaskBaselineViewModel
{
    public DateTime BaselineStart { get; set; }
    public DateTime BaselineEnd { get; set; }
    public string? BaselineAssignedUserName { get; set; }
    public string? BaselineEquipmentName { get; set; }
}

public class PlanChangeOrderViewModel
{
    public Guid Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public DateTime OldStartTime { get; set; }
    public DateTime NewStartTime { get; set; }
    public DateTime OldEndTime { get; set; }
    public DateTime NewEndTime { get; set; }
    public string? OldAssignedUserName { get; set; }
    public string? NewAssignedUserName { get; set; }
    public string? OldEquipmentName { get; set; }
    public string? NewEquipmentName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ApprovedByName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class OperationPlanCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên kế hoạch")]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string PlanType { get; set; } = "Daily";

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    public string? Notes { get; set; }
}

public class PlanTaskCreateViewModel
{
    public Guid PlanId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên công việc")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public Guid? AssignedUserId { get; set; }
    public Guid? EquipmentId { get; set; }

    public List<SelectOption> Users { get; set; } = new();
    public List<SelectOption> Equipments { get; set; } = new();
}
