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
    public List<MaintenanceRecordItem> MaintenanceRecords { get; set; } = new();
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
    public decimal? PurchasePrice { get; set; }
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
