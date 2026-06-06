using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

/// <summary>Ca làm việc (Shift)</summary>
public class WorkShift : TenantEntity
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty; // Ca sáng, Ca chiều, Ca đêm...

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>Số giờ làm tiêu chuẩn</summary>
    public double WorkHours { get; set; }

    [StringLength(50)]
    public string ShiftType { get; set; } = "Regular"; // Regular, Overtime, Night

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
}

/// <summary>Phân công ca làm việc cho nhân viên</summary>
public class ShiftAssignment : TenantEntity
{
    public Guid ShiftId { get; set; }
    public WorkShift? Shift { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public DateOnly WorkDate { get; set; }

    public ShiftAssignmentStatus Status { get; set; } = ShiftAssignmentStatus.Scheduled;

    public byte[] RowVersion { get; set; } = [];

    public TimeOnly? ActualCheckIn { get; set; }
    public TimeOnly? ActualCheckOut { get; set; }

    public string? Notes { get; set; }
}

/// <summary>Chứng chỉ / Năng lực nhân viên</summary>
public class EmployeeCertificate : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [Required, StringLength(200)]
    public string CertificateName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? IssuingOrganization { get; set; }

    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    [StringLength(100)]
    public string? CertificateNumber { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = "Professional"; // Professional, Safety, Technical, Language

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>Lịch sử bảo trì thiết bị</summary>
public class MaintenanceRecord : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Preventive;

    [Required]
    public DateOnly ScheduledDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public Guid? TechnicianUserId { get; set; }
    public AppUser? TechnicianUser { get; set; }

    public MaintenanceRecordStatus Status { get; set; } = MaintenanceRecordStatus.Scheduled;

    public string? Description { get; set; }
    public string? WorkDone { get; set; }

    public decimal? Cost { get; set; }

    public DateOnly? NextMaintenanceDate { get; set; }
}

/// <summary>Cơ sở hạ tầng - Mặt bằng / Phòng / Khu vực</summary>
public class Workspace : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = "Room"; // Room, Floor, Building, Zone, Desk, Workshop

    public Guid? ParentId { get; set; }
    public Workspace? Parent { get; set; }

    [StringLength(200)]
    public string? Location { get; set; } // Địa điểm, tầng, tòa nhà

    public double? AreaSqm { get; set; } // Diện tích m2

    public int? Capacity { get; set; } // Sức chứa tối đa

    public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Active;

    public string? Notes { get; set; }

    public ICollection<Workspace> Children { get; set; } = new List<Workspace>();
}
