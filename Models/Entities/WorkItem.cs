using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class WorkItem : TenantEntity
{
    public Guid OperationRequestId { get; set; }
    public OperationRequest? OperationRequest { get; set; }

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.Todo;

    /// <summary>Dynamic Kanban column — replaces the hardcoded Status enum for board display.</summary>
    public Guid? KanbanColumnId { get; set; }
    public KanbanColumn? KanbanColumn { get; set; }

    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public DateOnly? DueDate { get; set; }

    public ICollection<WorkItemAssignment> Assignments { get; set; } = new List<WorkItemAssignment>();
    public ICollection<WorkItemChecklist> Checklists { get; set; } = new List<WorkItemChecklist>();
    public ICollection<WorkItemComment> Comments { get; set; } = new List<WorkItemComment>();
}
