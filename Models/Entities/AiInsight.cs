using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class AiInsight : TenantEntity
{
    public Guid? AiPromptTemplateId { get; set; }
    public AiPromptTemplate? AiPromptTemplate { get; set; }

    public Guid? AiProviderConfigurationId { get; set; }
    public AiProviderConfiguration? AiProviderConfiguration { get; set; }

    public Guid? AskedByUserId { get; set; }
    public AppUser? AskedByUser { get; set; }

    [StringLength(80)]
    public string ContextType { get; set; } = string.Empty;

    public Guid? ContextId { get; set; }

    [StringLength(1000)]
    public string Question { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Summary { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Recommendation { get; set; }

    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    public AiInsightStatus Status { get; set; } = AiInsightStatus.Draft;

    public string? RawResponseJson { get; set; }
}
