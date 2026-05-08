using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkflowHistory : TenantEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }

    public Guid? FromStepId { get; set; }
    public WorkflowStep? FromStep { get; set; }

    public Guid? ToStepId { get; set; }
    public WorkflowStep? ToStep { get; set; }

    [StringLength(80)]
    public string ActionCode { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Note { get; set; }

    public Guid PerformedByUserId { get; set; }
    public AppUser? PerformedByUser { get; set; }
}
