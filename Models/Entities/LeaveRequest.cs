using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class LeaveRequest : TenantEntity
{
    public Guid EmployeeProfileId { get; set; }
    public EmployeeProfile? EmployeeProfile { get; set; }

    public LeaveType LeaveType { get; set; } = LeaveType.Annual;
    public LeaveStatus Status { get; set; } = LeaveStatus.Draft;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(500)]
    public string? RejectReason { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public AppUser? ApprovedByUser { get; set; }
}
