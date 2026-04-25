using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class AiMessage
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string Role { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public string MetadataJson { get; set; } = null!;

    public string CitationsJson { get; set; } = null!;

    public string? ChartsDataJson { get; set; }

    public int TokensUsed { get; set; }

    public string? Model { get; set; }

    public int? LatencyMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AiChatSession Session { get; set; } = null!;
}
