using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class NotificationPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string NotificationType { get; set; } = null!;

    public bool InAppEnabled { get; set; }

    public bool EmailEnabled { get; set; }

    public string EmailDigest { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
