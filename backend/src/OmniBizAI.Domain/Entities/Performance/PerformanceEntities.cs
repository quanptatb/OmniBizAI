using OmniBizAI.Domain.Common;
using OmniBizAI.Domain.Entities.Organization;
using OmniBizAI.Domain.Enums;
using OmniBizAI.Domain.Rules;

namespace OmniBizAI.Domain.Entities.Performance;

public sealed class EvaluationPeriod : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Monthly";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Planning";
    public decimal OkrWeight { get; set; } = 50;
    public decimal KpiWeight { get; set; } = 50;
    public string CheckInFrequency { get; set; } = "Weekly";
}

public sealed class Objective : SoftDeletableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid PeriodId { get; set; }
    public EvaluationPeriod Period { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public Objective? Parent { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OwnerType OwnerType { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? OwnerId { get; set; }
    public Employee? Owner { get; set; }
    public decimal Progress { get; set; }
    public string Status { get; set; } = "Draft";
    public string Priority { get; set; } = "Medium";
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public ICollection<KeyResult> KeyResults { get; set; } = new List<KeyResult>();
    public ICollection<Objective> Children { get; set; } = new List<Objective>();
}

public sealed class KeyResult : AuditableEntity
{
    public Guid ObjectiveId { get; set; }
    public Objective Objective { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MetricType MetricType { get; set; }
    public string? Unit { get; set; }
    public decimal StartValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Progress { get; set; }
    public decimal Weight { get; set; }
    public ProgressDirection Direction { get; set; } = ProgressDirection.Increase;
    public string Status { get; set; } = "NotStarted";
    public Guid? AssigneeId { get; set; }
    public Employee? Assignee { get; set; }

    public void RecalculateProgress()
    {
        Progress = PerformanceRules.CalculateProgress(StartValue, TargetValue, CurrentValue, Direction);
    }
}

public sealed class Kpi : SoftDeletableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid PeriodId { get; set; }
    public EvaluationPeriod Period { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? AssigneeId { get; set; }
    public Employee? Assignee { get; set; }
    public MetricType MetricType { get; set; }
    public string? Unit { get; set; }
    public decimal StartValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Weight { get; set; }
    public decimal Progress { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public string? Formula { get; set; }
    public ProgressDirection Direction { get; set; } = ProgressDirection.Increase;
    public string Status { get; set; } = "Active";
    public decimal? Score { get; set; }
    public string? Rating { get; set; }
    public DateTime? LastCheckInAt { get; set; }
    public ICollection<KpiCheckIn> CheckIns { get; set; } = new List<KpiCheckIn>();

    public void ApplyApprovedCheckIn(decimal newValue)
    {
        CurrentValue = newValue;
        Progress = PerformanceRules.CalculateProgress(StartValue, TargetValue, CurrentValue, Direction);
        Score = Progress;
        Rating = PerformanceRules.Rating(Progress);
        LastCheckInAt = DateTime.UtcNow;
    }
}

public sealed class KpiCheckIn : BaseEntity
{
    public Guid KpiId { get; set; }
    public Kpi Kpi { get; set; } = null!;
    public DateOnly CheckInDate { get; set; }
    public decimal? PreviousValue { get; set; }
    public decimal NewValue { get; set; }
    public decimal? Progress { get; set; }
    public string Note { get; set; } = string.Empty;
    public string EvidenceUrlsJson { get; set; } = "[]";
    public CheckInStatus Status { get; set; } = CheckInStatus.Submitted;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PerformanceEvaluation : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Guid PeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid? DepartmentId { get; set; }
    public decimal? OkrScore { get; set; }
    public decimal? KpiScore { get; set; }
    public decimal? TotalScore { get; set; }
    public string? Rating { get; set; }
    public string? ManagerComment { get; set; }
    public string Status { get; set; } = "Draft";
}
