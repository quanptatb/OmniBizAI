using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class Sprint : TenantEntity
{
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    [StringLength(1000)]
    public string? Goal { get; set; }

    public SprintStatus Status { get; set; } = SprintStatus.Planned;

    public byte[] RowVersion { get; set; } = [];

    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
}
