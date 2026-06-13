using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class OperationSlaPolicy : TenantEntity
{
    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    [Range(1, 8760)]
    public int MaxApprovalHours { get; set; } = 24;

    [Range(1, 8760)]
    public int MaxResolutionHours { get; set; } = 96;

    public bool IsActive { get; set; } = true;
}

public class OperationSlaBreach : TenantEntity
{
    public Guid OperationRequestId { get; set; }
    public OperationRequest? OperationRequest { get; set; }

    public OperationSlaBreachType BreachType { get; set; }

    public DateTimeOffset DueAt { get; set; }

    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;

    public decimal? HoursOverdue { get; set; }

    public bool IsEscalated { get; set; }

    public DateTimeOffset? NotificationSentAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
