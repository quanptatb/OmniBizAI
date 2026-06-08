using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>
/// Log thời gian sẵn sàng / hoạt động của thiết bị trong một ca.
/// Dùng cho Availability = (PlannedTime - Downtime) / PlannedTime.
/// </summary>
public class EquipmentAvailabilityLog : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public DateOnly LogDate { get; set; }

    /// <summary>Mã ca (CA-S/CA-C/CA-D) hoặc null nếu tính cả ngày.</summary>
    [StringLength(50)]
    public string? ShiftCode { get; set; }

    /// <summary>Thời gian hoạt động kế hoạch (phút).</summary>
    public int PlannedMinutes { get; set; }

    /// <summary>Thời gian dừng do mọi nguyên nhân (phút).</summary>
    public int DowntimeMinutes { get; set; }

    /// <summary>Thời gian chạy thực tế (phút) = Planned - Downtime.</summary>
    [NotMapped]
    public int RunMinutes => PlannedMinutes - DowntimeMinutes;
}

/// <summary>
/// Một lượt sản xuất trên thiết bị (run). Dùng cho Performance = ActualOutput / IdealOutput.
/// IdealOutput = IdealCycleSeconds * RunMinutes * 60.
/// </summary>
public class ProductionRun : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public Guid? PlanTaskId { get; set; }
    public PlanTask? PlanTask { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    [StringLength(200)]
    public string? ProductCode { get; set; }

    /// <summary>Số chu kỳ lý tưởng để sản xuất 1 đơn vị (giây).</summary>
    public decimal IdealCycleSeconds { get; set; }

    /// <summary>Tổng sản lượng đạt yêu cầu.</summary>
    public int GoodCount { get; set; }

    /// <summary>Tổng sản lượng phế phẩm.</summary>
    public int RejectCount { get; set; }

    /// <summary>Tổng = Good + Reject.</summary>
    [NotMapped]
    public int TotalCount => GoodCount + RejectCount;
}

/// <summary>
/// Kết quả chất lượng — chi tiết Reject để truy vết theo lỗi.
/// Quality = GoodCount / TotalCount.
/// </summary>
public class QualityResult : TenantEntity
{
    public Guid ProductionRunId { get; set; }
    public ProductionRun? ProductionRun { get; set; }

    public DateTimeOffset MeasuredAt { get; set; } = DateTimeOffset.UtcNow;

    [StringLength(100)]
    public string DefectType { get; set; } = string.Empty;

    public int DefectCount { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Sự kiện downtime cụ thể (sửa chữa, chờ vật tư, đổi ca, lỗi điện...).
/// Dùng để phân tích nguyên nhân và tính chính xác Availability.
/// </summary>
public class DowntimeEvent : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public Guid? MaintenanceIncidentId { get; set; }
    public MaintenanceIncident? MaintenanceIncident { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Phân loại: Breakdown, Setup, MaterialShortage, Quality, Planned, Other.</summary>
    [StringLength(50)]
    public string Category { get; set; } = "Breakdown";

    [StringLength(500)]
    public string? Description { get; set; }

    public int? DurationMinutes { get; set; }
}
