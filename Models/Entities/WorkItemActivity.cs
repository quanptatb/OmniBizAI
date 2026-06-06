using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkItemActivity : TenantEntity
{
    public Guid WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }

    public Guid? FromColumnId { get; set; }
    public KanbanColumn? FromColumn { get; set; }

    public Guid? ToColumnId { get; set; }
    public KanbanColumn? ToColumn { get; set; }

    public DateTimeOffset MovedAt { get; set; }

    public Guid MovedByUserId { get; set; }
    public AppUser? MovedByUser { get; set; }
}
