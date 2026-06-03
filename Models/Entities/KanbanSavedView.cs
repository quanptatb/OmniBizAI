using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class KanbanSavedView : TenantEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? SearchTerm { get; set; }

    public Guid? DepartmentId { get; set; }
    public Guid? SprintId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public PriorityLevel? Priority { get; set; }
    public Guid? TagId { get; set; }
    public DateOnly? DueFrom { get; set; }
    public DateOnly? DueTo { get; set; }
    public bool HasAttachment { get; set; }

    [StringLength(40)]
    public string? QuickFilter { get; set; }
}
