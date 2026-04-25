using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class AiGenerationHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CompanyId { get; set; }

    public string Module { get; set; } = null!;

    public string PromptType { get; set; } = null!;

    public string? InputSummary { get; set; }

    public string? InputDataJson { get; set; }

    public string OutputContent { get; set; } = null!;

    public string OutputType { get; set; } = null!;

    public string? Model { get; set; }

    public int? TokensUsed { get; set; }

    public int? Rating { get; set; }

    public string? Feedback { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
