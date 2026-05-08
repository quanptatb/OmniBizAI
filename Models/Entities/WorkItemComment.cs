using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkItemComment : TenantEntity
{
    public Guid WorkItemId { get; set; }
    public WorkItem? WorkItem { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
