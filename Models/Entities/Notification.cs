using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class Notification : TenantEntity
{
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    [StringLength(150)]
    public string? EntityName { get; set; }

    public Guid? EntityId { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Published;

    public DateTimeOffset? PublishedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}
