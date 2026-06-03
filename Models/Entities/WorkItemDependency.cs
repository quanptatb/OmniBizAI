using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class WorkItemDependency : TenantEntity
{
    public Guid BlockerId { get; set; }
    public WorkItem? Blocker { get; set; }

    public Guid BlockedId { get; set; }
    public WorkItem? Blocked { get; set; }

    public WorkItemDependencyType Type { get; set; } = WorkItemDependencyType.BlockedBy;
}
