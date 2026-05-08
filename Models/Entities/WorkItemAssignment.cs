using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkItemAssignment : TenantEntity
{
    public Guid WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }

    public Guid AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }
}
