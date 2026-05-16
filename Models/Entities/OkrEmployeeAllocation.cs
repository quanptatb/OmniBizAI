using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Allocates an OKR objective to an individual user.</summary>
public class OkrEmployeeAllocation : TenantEntity
{
    public Guid OkrObjectiveId { get; set; }
    public OkrObjective? OkrObjective { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
}
