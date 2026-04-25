using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual User User { get; set; } = null!;
}
