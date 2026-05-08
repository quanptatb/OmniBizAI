using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkItemChecklist : TenantEntity
{
    public Guid WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }

    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsCompleted { get; set; }

    public Guid? CompletedByUserId { get; set; }
    public AppUser? CompletedByUser { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
