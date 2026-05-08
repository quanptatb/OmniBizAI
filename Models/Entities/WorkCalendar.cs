using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class WorkCalendar : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public DateOnly WorkDate { get; set; }

    public bool IsWorkingDay { get; set; } = true;

    [StringLength(500)]
    public string? Note { get; set; }
}
