using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class BonusRule : TenantEntity
{
    public Guid GradingRankId { get; set; }
    public GradingRank? GradingRank { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? SalaryPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FixedAmount { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
