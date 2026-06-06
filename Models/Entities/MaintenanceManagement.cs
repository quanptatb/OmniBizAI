using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

/// <summary>Phụ tùng / Vật tư thay thế</summary>
public class SparePart : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Manufacturer { get; set; }

    [StringLength(100)]
    public string? PartNumber { get; set; } // Mã linh kiện của NSX

    [StringLength(100)]
    public string? Category { get; set; } // Bearing, Seal, Filter, Belt, Electrical...

    public int StockQuantity { get; set; } = 0;
    public int MinimumStock { get; set; } = 1; // Cảnh báo tồn kho thấp
    public decimal? UnitPrice { get; set; }

    [StringLength(20)]
    public string Unit { get; set; } = "Cái"; // Cái, Bộ, Mét, Lít...

    public string? Notes { get; set; }

    public ICollection<MaintenancePartUsage> PartUsages { get; set; } = new List<MaintenancePartUsage>();
    public ICollection<WorkOrderSparePartUsage> WorkOrderUsages { get; set; } = new List<WorkOrderSparePartUsage>();
    public ICollection<SparePartRequisitionLine> RequisitionLines { get; set; } = new List<SparePartRequisitionLine>();
}

/// <summary>Phụ tùng đã dùng trong một lần bảo trì</summary>
public class MaintenancePartUsage : TenantEntity
{
    public Guid MaintenanceRecordId { get; set; }
    public MaintenanceRecord? MaintenanceRecord { get; set; }

    public Guid SparePartId { get; set; }
    public SparePart? SparePart { get; set; }

    public int QuantityUsed { get; set; }
    public decimal? UnitCostAtTime { get; set; } // Giá tại thời điểm sử dụng
}

/// <summary>Kế hoạch Bảo trì Phòng ngừa (PM Schedule)</summary>
public class PmSchedule : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    [Required, StringLength(200)]
    public string TaskName { get; set; } = string.Empty; // Thay dầu, Kiểm tra lọc, Căn chỉnh...

    public PmFrequency Frequency { get; set; } = PmFrequency.Monthly;

    public int? FrequencyValue { get; set; } // Dùng khi Frequency = Every_X_Hours (ví dụ: 500 giờ)

    /// <summary>Loại trigger điều kiện PM (F5.4)</summary>
    public PmTriggerType TriggerType { get; set; } = PmTriggerType.TimeBased;

    /// <summary>Ngưỡng giờ chạy giữa 2 lần PM (RunHoursBased)</summary>
    public double? IntervalHours { get; set; }

    /// <summary>Ngưỡng số chu kỳ giữa 2 lần PM (CyclesBased)</summary>
    public long? IntervalCycles { get; set; }

    /// <summary>Snapshot Equipment.RunHours tại lần PM gần nhất</summary>
    public double? LastRunHoursAtPm { get; set; }

    /// <summary>Snapshot Equipment.CycleCount tại lần PM gần nhất</summary>
    public long? LastCyclesAtPm { get; set; }

    /// <summary>Sensor type theo dõi (ConditionBased)</summary>
    [StringLength(100)]
    public string? ConditionSensorType { get; set; }

    /// <summary>Ngưỡng cảnh báo cho ConditionBased (ví dụ vibration > 6mm/s)</summary>
    public double? ConditionThreshold { get; set; }

    [StringLength(200)]
    public string? Checklist { get; set; } // JSON hoặc text các bước kiểm tra

    public string? Instructions { get; set; } // Hướng dẫn chi tiết

    public DateOnly? LastPerformedDate { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? AssignedTechnicianId { get; set; }
    public AppUser? AssignedTechnician { get; set; }
}

/// <summary>Sự cố / Hỏng hóc thiết bị (Failure/Incident)</summary>
public class MaintenanceIncident : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; } // Mô tả chi tiết sự cố

    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset? OccurredAt { get; set; } // Thời điểm xảy ra sự cố

    public Guid? ReportedByUserId { get; set; }
    public AppUser? ReportedByUser { get; set; }

    public Guid? AssignedTechnicianId { get; set; }
    public AppUser? AssignedTechnician { get; set; }

    /// <summary>Link tới lệnh bảo trì được tạo để xử lý sự cố này</summary>
    public Guid? MaintenanceRecordId { get; set; }
    public MaintenanceRecord? MaintenanceRecord { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }
    public string? RootCause { get; set; } // Nguyên nhân gốc rễ
    public string? Resolution { get; set; } // Cách giải quyết
    public decimal? DowntimeHours { get; set; } // Thời gian ngừng máy (giờ)

    /// <summary>Mode lỗi (F5.6) — bắt buộc khi Resolve</summary>
    public Guid? FailureModeId { get; set; }
    public FailureMode? FailureMode { get; set; }

    /// <summary>5-Why JSON cho RCA — mảng 5 câu trả lời</summary>
    [StringLength(4000)]
    public string? FiveWhysJson { get; set; }

    /// <summary>Đánh dấu incident sinh từ sensor anomaly (F5.5)</summary>
    public bool IsAnomalyDetected { get; set; }
}

/// <summary>Dữ liệu IoT / Cảm biến của thiết bị (Giả lập)</summary>
public class EquipmentSensorReading : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    [StringLength(100)]
    public string SensorType { get; set; } = "Temperature"; // Temperature, Vibration, Pressure, RPM, Current

    public double Value { get; set; }

    [StringLength(20)]
    public string Unit { get; set; } = "°C"; // °C, mm/s, bar, rpm, A

    public DateTimeOffset ReadingTime { get; set; } = DateTimeOffset.UtcNow;

    public SensorReadingStatus Status { get; set; } = SensorReadingStatus.Normal;

    public double? ThresholdWarning { get; set; }
    public double? ThresholdCritical { get; set; }
}
