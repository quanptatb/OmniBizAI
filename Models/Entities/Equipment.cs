using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class Equipment : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string Type { get; set; } = string.Empty; // Máy móc, Xe cộ, Thiết bị IT...

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

    public byte[] RowVersion { get; set; } = [];

    [StringLength(200)]
    public string? Location { get; set; } // Vị trí hiện tại

    [StringLength(100)]
    public string? Manufacturer { get; set; } // Nhà sản xuất

    [StringLength(100)]
    public string? Model { get; set; } // Model thiết bị

    [StringLength(100)]
    public string? SerialNumber { get; set; } // Số serial

    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public int? LifespanYears { get; set; } // Tuổi thọ dự kiến (năm)
    public DateOnly? NextMaintenanceDate { get; set; }

    /// <summary>Tổng giờ chạy tích lũy (cho PM theo RunHours)</summary>
    public double RunHours { get; set; }

    /// <summary>Tổng chu kỳ/lượt vận hành (cho PM theo Cycles)</summary>
    public long CycleCount { get; set; }

    public string? Notes { get; set; }

    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public ICollection<EquipmentStatusHistory> StatusHistories { get; set; } = new List<EquipmentStatusHistory>();
    public ICollection<EquipmentCostLedger> CostLedgers { get; set; } = new List<EquipmentCostLedger>();
}
