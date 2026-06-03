using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.ViewModels;



// ─── INCIDENTS (CM) ───────────────────────────────────────────────────────────
public class IncidentSummaryItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? OccurredAt { get; set; }
    public string? TechnicianName { get; set; }
    public decimal? DowntimeHours { get; set; }
}

public class MaintenanceIncidentDetailViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReportedByName { get; set; }
    public string? TechnicianName { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? RootCause { get; set; }
    public string? Resolution { get; set; }
    public decimal? DowntimeHours { get; set; }
    public Guid? MaintenanceRecordId { get; set; }
    public string? AiAnalysis { get; set; }
    public List<string> NextStatuses { get; set; } = new();
    public bool CanResolve => NextStatuses.Count > 0 ? NextStatuses.Contains("Resolved") : Status != "Resolved" && Status != "Closed";
    public bool IsAnomalyDetected { get; set; }

    // F5.6
    public Guid? FailureModeId { get; set; }
    public string? FailureModeName { get; set; }
    public List<string> FiveWhys { get; set; } = new();
    public List<SelectOption> FailureModeOptions { get; set; } = new();
}

public class IncidentCreateFormViewModel
{
    public List<SelectOption> Equipments { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
}

public class IncidentCreateViewModel
{
    [Required(ErrorMessage = "Chọn thiết bị xảy ra sự cố")]
    public Guid EquipmentId { get; set; }

    [Required(ErrorMessage = "Nhập mô tả sự cố")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    public DateTimeOffset? OccurredAt { get; set; }
    public Guid? AssignedTechnicianId { get; set; }

    public List<SelectOption> Equipments { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
}

public class ResolveIncidentViewModel
{
    public Guid IncidentId { get; set; }
    public string? RootCause { get; set; }
    [Required] public string Resolution { get; set; } = string.Empty;
    public decimal? DowntimeHours { get; set; }

    // F5.6 — bắt buộc chọn FailureMode khi Resolve
    public Guid? FailureModeId { get; set; }
    // 5-Why
    public string? Why1 { get; set; }
    public string? Why2 { get; set; }
    public string? Why3 { get; set; }
    public string? Why4 { get; set; }
    public string? Why5 { get; set; }
}

// ─── PM SCHEDULES ─────────────────────────────────────────────────────────────
public class PmScheduleSummaryItem
{
    public Guid Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int? FrequencyValue { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public DateOnly? LastPerformedDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsOverdue { get; set; }
    public string? TechnicianName { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
}

public class PmScheduleCreateFormViewModel
{
    public List<SelectOption> Equipments { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
}

public class PmScheduleCreateViewModel
{
    [Required] public Guid EquipmentId { get; set; }
    [Required] public string TaskName { get; set; } = string.Empty;
    [Required] public string Frequency { get; set; } = "Monthly";
    public int? FrequencyValue { get; set; }
    public string? Instructions { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DateOnly? FirstDueDate { get; set; }
    public Guid? AssignedTechnicianId { get; set; }

    // F5.4 - Condition-based trigger
    public PmTriggerType TriggerType { get; set; } = PmTriggerType.TimeBased;
    public double? IntervalHours { get; set; }
    public long? IntervalCycles { get; set; }
    public string? ConditionSensorType { get; set; }
    public double? ConditionThreshold { get; set; }

    public List<SelectOption> Equipments { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
}

public class ExecutePmViewModel
{
    public Guid PmScheduleId { get; set; }
    [Required] public DateOnly CompletedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? WorkDone { get; set; }
    public decimal? Cost { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public Guid? TechnicianUserId { get; set; }
    public List<SelectOption> Technicians { get; set; } = new();
}

// ─── SPARE PARTS ──────────────────────────────────────────────────────────────
public class SparePartItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public int StockQuantity { get; set; }
    public int MinimumStock { get; set; }
    public decimal? UnitPrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsLowStock { get; set; }
}

public class SparePartCreateViewModel
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
    public string? Category { get; set; }
    public int InitialStock { get; set; } = 0;
    [Required] public int MinimumStock { get; set; } = 1;
    public decimal? UnitPrice { get; set; }
    [Required] public string Unit { get; set; } = "Cái";
    public string? Notes { get; set; }
}

public class StockAdjustViewModel
{
    public Guid PartId { get; set; }
    public int Delta { get; set; } // + nhập, - xuất
    public string Reason { get; set; } = string.Empty;
}

// ─── IoT / SENSOR ─────────────────────────────────────────────────────────────
public class SensorReadingViewModel
{
    public string SensorType { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset ReadingTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? ThresholdWarning { get; set; }
    public double? ThresholdCritical { get; set; }
}

// ─── WORK ORDER (F5.1 / F5.2 / F5.8) ──────────────────────────────────────────

public class WorkOrderListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public WorkOrderType Type { get; set; }
    public WorkOrderStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public string? TechnicianName { get; set; }
    public DateTimeOffset? ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? ActualCost { get; set; }
}

public class WorkOrderListViewModel
{
    public List<WorkOrderListItem> Items { get; set; } = new();
    public WorkOrderStatus? StatusFilter { get; set; }
    public Guid? EquipmentFilter { get; set; }
    public Guid? TechnicianFilter { get; set; }
    public int OpenCount { get; set; }
    public int AssignedCount { get; set; }
    public int InProgressCount { get; set; }
    public int OnHoldCount { get; set; }
    public int CompletedCount { get; set; }
    public List<SelectOption> Equipments { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
}

public class WorkOrderChecklistItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }
    public string? Notes { get; set; }
}

public class WorkOrderPartUsageViewModel
{
    public Guid Id { get; set; }
    public Guid SparePartId { get; set; }
    public string SparePartCode { get; set; } = string.Empty;
    public string SparePartName { get; set; } = string.Empty;
    public int QuantityUsed { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? LineTotal { get; set; }
}

public class WorkOrderSparePartOption
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

public class WorkOrderDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public WorkOrderType Type { get; set; }
    public WorkOrderStatus Status { get; set; }
    public PriorityLevel Priority { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string? RequestedByName { get; set; }
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTimeOffset? ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }
    public DateTimeOffset? ActualStart { get; set; }
    public DateTimeOffset? ActualEnd { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? WorkDone { get; set; }
    public Guid? IncidentId { get; set; }
    public string? IncidentTitle { get; set; }
    public Guid? PmScheduleId { get; set; }
    public string? PmTaskName { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedByName { get; set; }
    public List<WorkOrderChecklistItemViewModel> ChecklistItems { get; set; } = new();
    public List<WorkOrderPartUsageViewModel> PartUsages { get; set; } = new();
    public List<WorkOrderStatus> NextStatuses { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
    public List<WorkOrderSparePartOption> AvailableSpareParts { get; set; } = new();

    public decimal TotalPartsCost => PartUsages.Sum(p => p.LineTotal ?? 0);
    public int ChecklistDoneCount => ChecklistItems.Count(c => c.IsCompleted);
    public int ChecklistTotal => ChecklistItems.Count;
}

public class WorkOrderCreateFormViewModel
{
    [Required(ErrorMessage = "Chọn thiết bị")]
    public Guid EquipmentId { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public WorkOrderType Type { get; set; } = WorkOrderType.Corrective;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;
    public Guid? AssignedTechnicianId { get; set; }

    public DateTimeOffset? ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? EstimatedCost { get; set; }

    public Guid? IncidentId { get; set; }
    public Guid? PmScheduleId { get; set; }

    public List<string> ChecklistTitles { get; set; } = new();

    public List<SelectOption> Equipments { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
}

public class WorkOrderCompleteViewModel
{
    public Guid WorkOrderId { get; set; }
    public DateTimeOffset? ActualStart { get; set; }
    public decimal? ActualHours { get; set; }
    public decimal? LaborCost { get; set; }
    [Required, StringLength(2000)] public string WorkDone { get; set; } = string.Empty;
}

// ─── SPARE PART REQUISITION (F5.3) ────────────────────────────────────────────

public class SparePartRequisitionListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public SparePartRequisitionStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RequestedByName { get; set; }
    public string? LinkedWorkOrderCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int LineCount { get; set; }
    public int TotalQuantity { get; set; }
    public bool IsAutoReorder { get; set; }
}

public class SparePartRequisitionListViewModel
{
    public List<SparePartRequisitionListItem> Items { get; set; } = new();
    public SparePartRequisitionStatus? StatusFilter { get; set; }
    public int DraftCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int IssuedCount { get; set; }
}

public class SparePartRequisitionLineViewModel
{
    public Guid Id { get; set; }
    public Guid SparePartId { get; set; }
    public string SparePartCode { get; set; } = string.Empty;
    public string SparePartName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int StockOnHand { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}

public class SparePartRequisitionDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public SparePartRequisitionStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RequestedByName { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? LinkedWorkOrderId { get; set; }
    public string? LinkedWorkOrderCode { get; set; }
    public string? IssuedGoodsIssueNo { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public bool IsAutoReorder { get; set; }
    public List<SparePartRequisitionLineViewModel> Lines { get; set; } = new();
    public List<SparePartRequisitionStatus> NextStatuses { get; set; } = new();
}

public class SparePartRequisitionFormViewModel
{
    [Required(ErrorMessage = "Cần lý do")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public Guid? LinkedWorkOrderId { get; set; }

    public List<SparePartRequisitionLineViewModel> Lines { get; set; } = new();
    public List<WorkOrderSparePartOption> Parts { get; set; } = new();
    public List<SelectOption> WorkOrders { get; set; } = new();
}

// ─── FAILURE MODE (F5.6) ──────────────────────────────────────────────────────

public class FailureModeItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FailureModeCategory Category { get; set; }
    public string? Description { get; set; }
    public string? TypicalPreventionMeasure { get; set; }
    public bool IsActive { get; set; }
    public int IncidentCount { get; set; }
}

public class FailureModeEditViewModel
{
    public Guid? Id { get; set; }
    [StringLength(50)] public string? Code { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    public FailureModeCategory Category { get; set; } = FailureModeCategory.Mechanical;
    [StringLength(2000)] public string? Description { get; set; }
    [StringLength(2000)] public string? TypicalPreventionMeasure { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FailureModeStatItem
{
    public Guid FailureModeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FailureModeCategory Category { get; set; }
    public int IncidentCount { get; set; }
    public decimal TotalDowntimeHours { get; set; }
}

public class FailureModeStatisticsViewModel
{
    public int Months { get; set; }
    public int TotalIncidents { get; set; }
    public int TaggedIncidents { get; set; }
    public List<FailureModeStatItem> TopFailureModes { get; set; } = new();
    public double TaggingRate => TotalIncidents == 0 ? 0 : (double)TaggedIncidents / TotalIncidents * 100.0;
}
