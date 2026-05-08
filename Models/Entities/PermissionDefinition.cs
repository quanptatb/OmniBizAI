using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class PermissionDefinition : BaseEntity
{
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(80)]
    public string ModuleCode { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<PermissionAssignment> Assignments { get; set; } = new List<PermissionAssignment>();
}
