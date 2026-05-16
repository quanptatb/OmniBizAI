using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Stores history of AI generation requests and responses for caching and audit.</summary>
public class AiGenerationHistory : TenantEntity
{
    [StringLength(100)]
    public string FeatureType { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ModelName { get; set; }

    public string? PromptText { get; set; }

    public string? ResponseText { get; set; }

    public int? TokensUsed { get; set; }

    public Guid? RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }
}
