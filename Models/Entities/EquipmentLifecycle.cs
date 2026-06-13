using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class EquipmentStatusHistory : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public EquipmentStatus? OldStatus { get; set; }
    public EquipmentStatus NewStatus { get; set; }

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ChangedByUserId { get; set; }
    public AppUser? ChangedByUser { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public class EquipmentCostLedger : TenantEntity
{
    public Guid EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public EquipmentCostType CostType { get; set; } = EquipmentCostType.Other;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateOnly OccurredDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(100)]
    public string? SourceType { get; set; }

    public Guid? SourceId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
