using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

/// <summary>Lệnh công tác bảo trì (F5.1)</summary>
public class WorkOrder : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public WorkOrderType Type { get; set; } = WorkOrderType.Corrective;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public byte[] RowVersion { get; set; } = [];

    [Required, StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public Guid? RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public Guid? AssignedTechnicianId { get; set; }
    public AppUser? AssignedTechnician { get; set; }

    public DateTimeOffset? ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }
    public DateTimeOffset? ActualStart { get; set; }
    public DateTimeOffset? ActualEnd { get; set; }

    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ActualCost { get; set; }

    public string? WorkDone { get; set; }

    /// <summary>Sinh từ Incident (CM)</summary>
    public Guid? IncidentId { get; set; }
    public MaintenanceIncident? Incident { get; set; }

    /// <summary>Sinh từ PM Schedule</summary>
    public Guid? PmScheduleId { get; set; }
    public PmSchedule? PmSchedule { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public AppUser? CompletedByUser { get; set; }

    public ICollection<WorkOrderChecklistItem> ChecklistItems { get; set; } = new List<WorkOrderChecklistItem>();
    public ICollection<WorkOrderSparePartUsage> PartUsages { get; set; } = new List<WorkOrderSparePartUsage>();
}

/// <summary>Item checklist trong Work Order</summary>
public class WorkOrderChecklistItem : TenantEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public AppUser? CompletedByUser { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

/// <summary>Phụ tùng dùng cho Work Order (F5.2)</summary>
public class WorkOrderSparePartUsage : TenantEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    public Guid SparePartId { get; set; }
    public SparePart? SparePart { get; set; }

    public int QuantityUsed { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LineTotal { get; set; }

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
