using OmniBizAI.Domain.Common;

namespace OmniBizAI.Domain.Entities.AI;

public sealed class AiChatSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public string ContextType { get; set; } = "General";
    public string ContextDataJson { get; set; } = "{}";
    public int MessageCount { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}

public sealed class AiMessage : BaseEntity
{
    public Guid SessionId { get; set; }
    public AiChatSession Session { get; set; } = null!;
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text";
    public string MetadataJson { get; set; } = "{}";
    public string CitationsJson { get; set; } = "[]";
    public string? ChartsDataJson { get; set; }
    public int TokensUsed { get; set; }
    public string? Model { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AiGenerationHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string PromptType { get; set; } = string.Empty;
    public string? InputSummary { get; set; }
    public string? InputDataJson { get; set; }
    public string OutputContent { get; set; } = string.Empty;
    public string OutputType { get; set; } = "text";
    public string? Model { get; set; }
    public int? TokensUsed { get; set; }
    public int? Rating { get; set; }
    public string? Feedback { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

public sealed class AiRiskAssessment : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public string RiskFactorsJson { get; set; } = "[]";
    public string RecommendationsJson { get; set; } = "[]";
    public string? Model { get; set; }
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public string AssessedBy { get; set; } = "system";
}
