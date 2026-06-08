using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class PlanTask : TenantEntity
{
    public Guid PlanId { get; set; }
    public OperationPlan? Plan { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public Guid? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }

    public Guid? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    [Required, StringLength(50)]
    public string Status { get; set; } = "Todo"; // Todo, InProgress, Done, Delayed

    [Range(0, 100)]
    public int ProgressPercent { get; set; } = 0;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
