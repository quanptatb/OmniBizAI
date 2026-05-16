using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class GradingRank : TenantEntity
{
    public GradingRankCode RankCode { get; set; }

    [StringLength(50)]
    public string RankName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal MinScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxScore { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
}
