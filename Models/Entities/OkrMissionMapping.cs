using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Many-to-many join: OKR ↔ MissionVision.</summary>
public class OkrMissionMapping : TenantEntity
{
    public Guid OkrObjectiveId { get; set; }
    public OkrObjective? OkrObjective { get; set; }

    public Guid MissionVisionId { get; set; }
    public MissionVision? MissionVision { get; set; }
}
