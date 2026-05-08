using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class NotificationDelivery : TenantEntity
{
    public Guid NotificationId { get; set; }
    public Notification? Notification { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;

    public DateTimeOffset? DeliveredAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}
