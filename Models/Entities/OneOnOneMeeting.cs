using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>One-on-one meeting between a manager and an employee.</summary>
public class OneOnOneMeeting : TenantEntity
{
    public Guid ManagerUserId { get; set; }
    public AppUser? ManagerUser { get; set; }

    public Guid EmployeeUserId { get; set; }
    public AppUser? EmployeeUser { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }

    [StringLength(500)]
    public string? Agenda { get; set; }

    [StringLength(3000)]
    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }
}
