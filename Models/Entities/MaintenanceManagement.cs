using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

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

    [Timestamp]
    public byte[]? RowVersion { get; set; }
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

    [Required, StringLength(50)]
    public string Frequency { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Quarterly, Yearly, Every_X_Hours

    public int? FrequencyValue { get; set; } // Dùng khi Frequency = Every_X_Hours (ví dụ: 500 giờ)

    [StringLength(200)]
    public string? Checklist { get; set; } // JSON hoặc text các bước kiểm tra

    public string? Instructions { get; set; } // Hướng dẫn chi tiết

    public DateOnly? LastPerformedDate { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? AssignedTechnicianId { get; set; }
    public AppUser? AssignedTechnician { get; set; }

    /// <summary>Lần gần nhất gửi notification overdue/due-soon (để chống spam).</summary>
    public DateTimeOffset? LastOverdueNotificationAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

/// <summary>Sự cố / Hỏng hóc thiết bị (Failure/Incident)</summary>
public class MaintenanceIncident : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; } // Mô tả chi tiết sự cố

    [StringLength(50)]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    [StringLength(50)]
    public string Status { get; set; } = "Open"; // Open, InProgress, Resolved, Closed

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

    public decimal? PartsCost { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? TotalCost { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
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

    [StringLength(50)]
    public string Status { get; set; } = "Normal"; // Normal, Warning, Critical

    public double? ThresholdWarning { get; set; }
    public double? ThresholdCritical { get; set; }
}
