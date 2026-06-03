using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class PlanTaskBaseline : TenantEntity
{
    public Guid PlanId { get; set; }
    public OperationPlan? Plan { get; set; }

    public Guid PlanTaskId { get; set; }
    public PlanTask? PlanTask { get; set; }

    [Required, StringLength(200)]
    public string TaskName { get; set; } = string.Empty;

    public DateTime BaselineStart { get; set; }
    public DateTime BaselineEnd { get; set; }

    public Guid? BaselineAssignedUserId { get; set; }
    public AppUser? BaselineAssignedUser { get; set; }

    public Guid? BaselineEquipmentId { get; set; }
    public Equipment? BaselineEquipment { get; set; }

    public DateTimeOffset SnapshottedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? SnapshottedByUserId { get; set; }
    public AppUser? SnapshottedByUser { get; set; }
}
