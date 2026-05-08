using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class AiProviderConfiguration : TenantEntity
{
    [StringLength(80)]
    public string ProviderCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string ModelName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Endpoint { get; set; }

    [StringLength(200)]
    public string? ApiKeySecretName { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<AiInsight> Insights { get; set; } = new List<AiInsight>();
}
