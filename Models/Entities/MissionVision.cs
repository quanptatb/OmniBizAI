using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class MissionVision : TenantEntity
{
    public MissionVisionType Type { get; set; } = MissionVisionType.YearlyGoal;

    public int? TargetYear { get; set; }

    [StringLength(4000)]
    public string? Content { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FinancialTarget { get; set; }

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public string TypeDisplayName => Type switch
    {
        MissionVisionType.Vision => "Tầm nhìn",
        MissionVisionType.Mission => "Sứ mệnh",
        MissionVisionType.YearlyGoal => "Mục tiêu chiến lược theo năm",
        _ => "Mục tiêu chiến lược"
    };

    [NotMapped]
    public bool IsYearlyGoal => Type == MissionVisionType.YearlyGoal;
}
