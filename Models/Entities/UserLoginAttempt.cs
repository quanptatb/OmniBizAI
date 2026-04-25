using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class UserLoginAttempt
{
    public long Id { get; set; }

    public string Email { get; set; } = null!;

    public string? IpAddress { get; set; }

    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }

    public DateTime AttemptedAt { get; set; }
}
