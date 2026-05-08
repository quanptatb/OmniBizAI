using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class ApprovalTask : TenantEntity
{
    public Guid? WorkflowInstanceId { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }

    [StringLength(150)]
    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    [StringLength(80)]
    public string StepCode { get; set; } = string.Empty;

    public Guid? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    [StringLength(80)]
    public string? AssignedRole { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    [StringLength(1000)]
    public string? DecisionNote { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }
}
