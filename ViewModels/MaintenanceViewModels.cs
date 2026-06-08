using System.ComponentModel.DataAnnotations;

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
    public decimal? PartsCost { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? TotalCost { get; set; }
    public Guid? MaintenanceRecordId { get; set; }
    public string? AiAnalysis { get; set; }
    public List<PartUsageDisplay> PartsUsed { get; set; } = new();
    public List<SparePartOption> AvailableParts { get; set; } = new();
}

public class PartUsageDisplay
{
    public string PartCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? UnitCostAtTime { get; set; }
    public decimal? LineTotal => UnitCostAtTime * Quantity;
}

public class IncidentCreateFormViewModel
{
    public List<SelectOption> Equipments { get; set; } = new();
    public List<SelectOption> Technicians { get; set; } = new();
    public List<SparePartOption> AvailableParts { get; set; } = new();
}

public class SparePartOption
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal? UnitPrice { get; set; }
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
    public decimal? PartsCost { get; set; }
    public decimal? LaborCost { get; set; }

    /// <summary>Phụ tùng đã dùng để xử lý sự cố (sẽ tự trừ kho).</summary>
    public List<PartUsageInput> PartsUsed { get; set; } = new();
}

public class PartUsageInput
{
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
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
    public List<PartUsageInput> PartsUsed { get; set; } = new();
    public List<SparePartOption> AvailableParts { get; set; } = new();
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
