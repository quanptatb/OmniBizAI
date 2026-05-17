using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Tracks customer care interactions — calls, emails, meetings, notes.</summary>
public class CrmInteraction : TenantEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? CustomerContactId { get; set; }
    public CustomerContact? CustomerContact { get; set; }

    /// <summary>Call, Email, Meeting, Visit, Note, Complaint, Feedback</summary>
    [StringLength(50)]
    public string Type { get; set; } = "Note";

    [StringLength(250)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    /// <summary>Planned, InProgress, Completed, Cancelled</summary>
    [StringLength(50)]
    public string Status { get; set; } = "Planned";

    /// <summary>Low, Normal, High, Urgent</summary>
    [StringLength(30)]
    public string Priority { get; set; } = "Normal";

    public DateTimeOffset? ScheduledAt { get; set; }

    public int? DurationMinutes { get; set; }

    [StringLength(2000)]
    public string? Outcome { get; set; }

    [StringLength(2000)]
    public string? NextAction { get; set; }

    public DateTimeOffset? NextActionDate { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public Guid? CompletedByUserId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
