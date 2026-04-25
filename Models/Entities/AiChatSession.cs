using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class AiChatSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? Title { get; set; }

    public string ContextType { get; set; } = null!;

    public string ContextDataJson { get; set; } = null!;

    public int MessageCount { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AiMessage> AiMessages { get; set; } = new List<AiMessage>();
}
