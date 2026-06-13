using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class PlanChangeOrder : TenantEntity
{
    public Guid PlanId { get; set; }
    public OperationPlan? Plan { get; set; }

    public Guid PlanTaskId { get; set; }
    public PlanTask? PlanTask { get; set; }

    public DateTime OldStartTime { get; set; }
    public DateTime NewStartTime { get; set; }

    public DateTime OldEndTime { get; set; }
    public DateTime NewEndTime { get; set; }

    public Guid? OldAssignedUserId { get; set; }
    public AppUser? OldAssignedUser { get; set; }

    public Guid? NewAssignedUserId { get; set; }
    public AppUser? NewAssignedUser { get; set; }

    public Guid? OldEquipmentId { get; set; }
    public Equipment? OldEquipment { get; set; }

    public Guid? NewEquipmentId { get; set; }
    public Equipment? NewEquipment { get; set; }

    [Required, StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public PlanChangeOrderStatus Status { get; set; } = PlanChangeOrderStatus.Approved;

    public Guid? ApprovedByUserId { get; set; }
    public AppUser? ApprovedByUser { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
}
