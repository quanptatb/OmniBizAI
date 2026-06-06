using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class PlanTask : TenantEntity
{
    public Guid PlanId { get; set; }
    public OperationPlan? Plan { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public Guid? AssignedUserId { get; set; }
    public AppUser? AssignedUser { get; set; }

    public Guid? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public PlanTaskStatus Status { get; set; } = PlanTaskStatus.Todo;

    public byte[] RowVersion { get; set; } = [];

    [Range(0, 100)]
    public int ProgressPercent { get; set; } = 0;

    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public int? PlannedDurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitsProduced { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitsGood { get; set; }

    public decimal? OeeAvailabilityPercent { get; set; }
    public decimal? OeePerformancePercent { get; set; }
    public decimal? OeeQualityPercent { get; set; }
    public decimal? OeePercent { get; set; }

    public DateTime? EarlyStart { get; set; }
    public DateTime? EarlyFinish { get; set; }
    public DateTime? LateStart { get; set; }
    public DateTime? LateFinish { get; set; }
    public int? SlackMinutes { get; set; }
    public bool IsCriticalPath { get; set; }

    public ICollection<PlanChangeOrder> ChangeOrders { get; set; } = new List<PlanChangeOrder>();
    public ICollection<PlanTaskDependency> PredecessorDependencies { get; set; } = new List<PlanTaskDependency>();
    public ICollection<PlanTaskDependency> SuccessorDependencies { get; set; } = new List<PlanTaskDependency>();
}
