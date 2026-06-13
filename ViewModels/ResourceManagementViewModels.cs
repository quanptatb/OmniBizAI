using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities;

namespace OmniBizAI.ViewModels;

// ─── DASHBOARD ───────────────────────────────────────────────────────────────
public class ResourceDashboardViewModel
{
    public int EquipmentCount { get; set; }
    public int EquipmentInMaintenance { get; set; }
    public int OverdueMaintenanceCount { get; set; }
    public int ActiveShiftCount { get; set; }
    public int TodayAssignmentCount { get; set; }
    public int ExpiredCertificateCount { get; set; }
    public int ExpiringCertificateCount { get; set; }
    public int WorkspaceCount { get; set; }
    public List<MaintenanceAlertItem> UpcomingMaintenance { get; set; } = new();
    public List<EquipmentSummaryItem> RecentEquipments { get; set; } = new();
}

// ─── EQUIPMENT ───────────────────────────────────────────────────────────────
public class EquipmentSummaryItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    public int? LifespanYears { get; set; }
}

public class EquipmentDetailViewModel : EquipmentSummaryItem
{
    public string? SerialNumber { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Notes { get; set; }
    public EquipmentOeeSummaryViewModel Oee7Days { get; set; } = new();
    public EquipmentOeeSummaryViewModel Oee30Days { get; set; } = new();
    public EquipmentOeeSummaryViewModel Oee90Days { get; set; } = new();
    public List<EquipmentOeeTrendPointViewModel> OeeTrend { get; set; } = new();
    public List<EquipmentOeeTaskItemViewModel> RecentOeeTasks { get; set; } = new();
    public EquipmentCostPerformanceViewModel CostPerformance { get; set; } = new();
    public List<EquipmentCostLedgerItemViewModel> CostLedgers { get; set; } = new();
    public List<EquipmentStatusHistoryItemViewModel> StatusHistories { get; set; } = new();
    public List<MaintenanceRecordItem> MaintenanceRecords { get; set; } = new();
}

public class EquipmentCostPerformanceViewModel
{
    public decimal PurchaseCost { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal RepairCost { get; set; }
    public decimal SparePartCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal TotalCost => PurchaseCost + MaintenanceCost + RepairCost + SparePartCost + OtherCost;
    public decimal DowntimeHours { get; set; }
    public int FailureCount { get; set; }
    public decimal? MtbfHours { get; set; }
    public decimal? MttrHours { get; set; }
    public bool ShouldRecommendReplace { get; set; }
    public decimal? CostToPurchasePercent { get; set; }
}

public class EquipmentCostLedgerItemViewModel
{
    public Guid Id { get; set; }
    public string CostType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly OccurredDate { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public string? Notes { get; set; }
}

public class EquipmentStatusHistoryItemViewModel
{
    public Guid Id { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string? Reason { get; set; }
    public string? ChangedByName { get; set; }
}

public class EquipmentOeeSummaryViewModel
{
    public int Days { get; set; }
    public int TaskCount { get; set; }
    public decimal? OeePercent { get; set; }
    public decimal? AvailabilityPercent { get; set; }
    public decimal? PerformancePercent { get; set; }
    public decimal? QualityPercent { get; set; }
    public decimal UnitsProduced { get; set; }
    public decimal UnitsGood { get; set; }
    public string OeeLabel => OeePercent.HasValue ? $"{OeePercent.Value:0.#}%" : "N/A";
}

public class EquipmentOeeTrendPointViewModel
{
    public DateOnly Date { get; set; }
    public decimal? OeePercent { get; set; }
    public int TaskCount { get; set; }
}

public class EquipmentOeeTaskItemViewModel
{
    public Guid Id { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public DateTime? ActualEndTime { get; set; }
    public int? PlannedDurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public decimal? UnitsProduced { get; set; }
    public decimal? UnitsGood { get; set; }
    public decimal? OeePercent { get; set; }
}

public class EquipmentCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên thiết bị")]
    public string Name { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = "Máy móc";
    public string? Location { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    [Range(0.0, double.MaxValue, ErrorMessage = "Giá mua không được là số âm")]
    public decimal? PurchasePrice { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Tuổi thọ không được là số âm")]
    public int? LifespanYears { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
    public string? Notes { get; set; }
}

// ─── MAINTENANCE ─────────────────────────────────────────────────────────────
public class MaintenanceRecordItem
{
    public Guid Id { get; set; }
    public string MaintenanceType { get; set; } = string.Empty;
    public DateOnly ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string? TechnicianName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WorkDone { get; set; }
    public decimal? Cost { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
}

public class MaintenanceAlertItem
{
    public Guid Id { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string MaintenanceType { get; set; } = string.Empty;
    public DateOnly ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ScheduleMaintenanceViewModel
{
    public Guid EquipmentId { get; set; }
    [Required] public string MaintenanceType { get; set; } = "Preventive";
    [Required] public DateOnly ScheduledDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? Description { get; set; }
    public Guid? TechnicianUserId { get; set; }
    public List<SelectOption> Technicians { get; set; } = new();
}

public class CompleteMaintenanceViewModel
{
    public Guid RecordId { get; set; }
    [Required] public DateOnly CompletedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? WorkDone { get; set; }
    public decimal? Cost { get; set; }
    public DateOnly? NextMaintenanceDate { get; set; }
}

// ─── WORK SHIFTS ─────────────────────────────────────────────────────────────
public class WorkShiftViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public double WorkHours { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public int TodayAssignmentCount { get; set; }
}

public class WorkShiftCreateViewModel
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public TimeOnly StartTime { get; set; }
    [Required] public TimeOnly EndTime { get; set; }
    public double WorkHours { get; set; } = 8;
    public string ShiftType { get; set; } = "Regular";
    public string? Notes { get; set; }
}

public class ShiftScheduleViewModel
{
    public DateOnly TargetDate { get; set; }
    public List<ShiftAssignmentItem> Assignments { get; set; } = new();
    public List<SelectOption> Shifts { get; set; } = new();
    public List<SelectOption> Users { get; set; } = new();
}

public class ShiftAssignmentItem
{
    public Guid Id { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly ShiftStart { get; set; }
    public TimeOnly ShiftEnd { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeOnly? ActualCheckIn { get; set; }
    public TimeOnly? ActualCheckOut { get; set; }
}

public class ResourceAvailabilityMatrixViewModel
{
    public DateOnly Date { get; set; }
    public int DurationHours { get; set; } = 1;
    public List<ResourceAvailabilityWorkerRowViewModel> Rows { get; set; } = new();
    public List<int> Hours => Enumerable.Range(0, 24).ToList();

    public int AvailableCount => Rows.SelectMany(r => r.Slots).Count(s => s.Status == "Available");
    public int BusyCount => Rows.SelectMany(r => r.Slots).Count(s => s.Status == "Busy");
    public int LeaveCount => Rows.SelectMany(r => r.Slots).Count(s => s.Status == "Leave");
    public int NoShiftCount => Rows.SelectMany(r => r.Slots).Count(s => s.Status == "NoShift");
}

public class ResourceAvailabilityWorkerRowViewModel
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public List<ResourceAvailabilitySlotViewModel> Slots { get; set; } = new();
}

public class ResourceAvailabilitySlotViewModel
{
    public int Hour { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string CssClass { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimeLabel => $"{StartTime:HH:mm}-{EndTime:HH:mm}";
}

public class AssignShiftViewModel
{
    public Guid ShiftId { get; set; }
    public Guid UserId { get; set; }
    [Required] public DateOnly WorkDate { get; set; }
}

// ─── CERTIFICATES ─────────────────────────────────────────────────────────────
public class EmployeeCertificateItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string CertificateName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? CertificateNumber { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExpiringSoon { get; set; }
}

public class CertificateCreateFormViewModel
{
    public List<SelectOption> Users { get; set; } = new();
}

public class CertificateCreateViewModel
{
    [Required] public Guid UserId { get; set; }
    [Required] public string CertificateName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Category { get; set; } = "Professional";
    public string? CertificateNumber { get; set; }
    public List<SelectOption> Users { get; set; } = new();
}

// ─── WORKSPACES ──────────────────────────────────────────────────────────────
public class WorkspaceItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Location { get; set; }
    public double? AreaSqm { get; set; }
    public int? Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class WorkspaceCreateViewModel
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = "Room";
    public string? Location { get; set; }
    public double? AreaSqm { get; set; }
    public int? Capacity { get; set; }
    public Guid? ParentId { get; set; }
    public string? Notes { get; set; }
    public List<SelectOption> ParentWorkspaces { get; set; } = new();
}
