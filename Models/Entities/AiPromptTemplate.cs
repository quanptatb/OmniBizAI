using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class AiPromptTemplate : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(80)]
    public string ContextType { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public string SystemPrompt { get; set; } = string.Empty;

    public string UserPromptTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<AiInsight> Insights { get; set; } = new List<AiInsight>();
}
