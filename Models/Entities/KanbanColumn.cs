using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>
/// A dynamic Kanban board column. Users can create/rename/reorder columns freely.
/// Each tenant has its own set of columns.
/// </summary>
public class KanbanColumn : TenantEntity
{
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>CSS accent class name for the column header color, e.g. "backlog", "todo", "doing".</summary>
    [StringLength(50)]
    public string AccentColor { get; set; } = "#94a3b8";

    /// <summary>Sort position on the board (0-based, ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>If true, this column represents a "completed" state and will close related work.</summary>
    public bool IsDoneColumn { get; set; }

    /// <summary>If true, this column represents a "cancelled/archived" state.</summary>
    public bool IsCancelledColumn { get; set; }

    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
}
