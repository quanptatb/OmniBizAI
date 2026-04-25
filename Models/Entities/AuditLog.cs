using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class AuditLog
{
    public long Id { get; set; }

    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    public string Action { get; set; } = null!;

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public string? EntityName { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public string? ChangesSummary { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? RequestPath { get; set; }

    public string? RequestMethod { get; set; }

    public int? ResponseStatus { get; set; }

    public long? DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }
}
