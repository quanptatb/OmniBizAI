using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.ViewModels;

// ═══════════════════════════════════════════════════════════════════════════════
// OKR ViewModels
// ═══════════════════════════════════════════════════════════════════════════════

public class OkrListViewModel
{
    public List<OkrListItem> Items { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? LevelFilter { get; set; }
    public string? StatusFilter { get; set; }
}

public class OkrListItem
{
    public Guid Id { get; set; }
    public string ObjectiveName { get; set; } = "";
    public string Level { get; set; } = "";
    public string Cycle { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsActive { get; set; }
    public int KeyResultCount { get; set; }
    public decimal Progress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class OkrDetailViewModel
{
    public Guid Id { get; set; }
    public string ObjectiveName { get; set; } = "";
    public string Level { get; set; } = "";
    public string Cycle { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<OkrKeyResultItem> KeyResults { get; set; } = new();
    public List<string> MissionLinks { get; set; } = new();
    public List<string> DepartmentLinks { get; set; } = new();
    public List<string> EmployeeLinks { get; set; } = new();
    public decimal TotalProgress => KeyResults.Any() ? Math.Round(KeyResults.Average(kr => kr.Progress), 1) : 0;
}

public class OkrKeyResultItem
{
    public Guid Id { get; set; }
    public string KeyResultName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public bool IsInverse { get; set; }
    public decimal Progress { get; set; }
}

public class OkrCreateViewModel
{
    [Required(ErrorMessage = "Tên mục tiêu không được để trống")]
    [StringLength(255)]
    public string ObjectiveName { get; set; } = string.Empty;

    public OkrLevel Level { get; set; } = OkrLevel.Company;

    [StringLength(50)]
    public string? Cycle { get; set; }

    public List<OkrKeyResultCreateItem>? KeyResults { get; set; }

    // Dropdowns
    public List<Guid> SelectedDepartmentIds { get; set; } = new();
    public List<Guid> SelectedMissionIds { get; set; } = new();

    // Dropdowns
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Missions { get; set; } = new();
}

public class OkrKeyResultCreateItem
{
    [Required]
    [StringLength(500)]
    public string KeyResultName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Unit { get; set; }

    public decimal TargetValue { get; set; }
    public bool IsInverse { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
// KPI ViewModels (full — from KPI project)
// ═══════════════════════════════════════════════════════════════════════════════

public class KpiFullListViewModel
{
    public List<KpiFullListItem> Items { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? PeriodFilter { get; set; }
    public string? OwnerTypeFilter { get; set; }
    public List<SelectOption> Periods { get; set; } = new();
}

public class KpiFullListItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public string OwnerType { get; set; } = "";
    public string MeasureType { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public string Status { get; set; } = "";
    public string Department { get; set; } = "";
    public string? OkrName { get; set; }
    public string? PeriodName { get; set; }
    public decimal TargetValue { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class KpiDetailViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Unit { get; set; } = "";
    public string OwnerType { get; set; } = "";
    public string MeasureType { get; set; } = "";
    public string PropertyType { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsActive { get; set; }
    public string? Department { get; set; }
    public string? OkrName { get; set; }
    public string? KeyResultName { get; set; }
    public string? PeriodName { get; set; }
    public string? AssignerName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<KpiTargetItem> Targets { get; set; } = new();
    public List<string> DepartmentAssignments { get; set; } = new();
    public List<KpiEmployeeAssignmentItem> EmployeeAssignments { get; set; } = new();
}

public class KpiTargetItem
{
    public Guid Id { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? PassThreshold { get; set; }
    public decimal? FailThreshold { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int? CheckInFrequencyDays { get; set; }
    public bool ReminderEnabled { get; set; }
}

public class KpiEmployeeAssignmentItem
{
    public string UserName { get; set; } = "";
    public decimal Weight { get; set; }
}

public class KpiCreateViewModel
{
    [Required(ErrorMessage = "Tên KPI không được để trống")]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Đơn vị đo không được để trống")]
    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;

    public KpiOwnerType OwnerType { get; set; } = KpiOwnerType.Department;
    public KpiPeriodType PeriodType { get; set; } = KpiPeriodType.Monthly;
    public KpiMeasureType MeasureType { get; set; } = KpiMeasureType.Quantitative;
    public KpiPropertyType PropertyType { get; set; } = KpiPropertyType.Growth;

    public Guid? OrganizationUnitId { get; set; }
    public Guid? OkrObjectiveId { get; set; }
    public Guid? OkrKeyResultId { get; set; }
    public Guid? EvaluationPeriodId { get; set; }

    // Target
    public decimal TargetValue { get; set; }
    public decimal? PassThreshold { get; set; }
    public decimal? FailThreshold { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public int? CheckInFrequencyDays { get; set; }
    public TimeOnly? DeadlineTime { get; set; }
    public bool ReminderEnabled { get; set; }

    // Dropdowns
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> OkrObjectives { get; set; } = new();
    public List<SelectOption> OkrKeyResults { get; set; } = new();
    public List<SelectOption> Periods { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════════
// KPI Check-In ViewModels
// ═══════════════════════════════════════════════════════════════════════════════

public class KpiCheckInListViewModel
{
    public List<KpiCheckInListItem> Items { get; set; } = new();
    public KpiCheckInSubmitViewModel SubmitForm { get; set; } = new();
    public List<SelectOption> AvailableTargets { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? SearchTerm { get; set; }
    public string? ReviewStatusFilter { get; set; }
}

public class KpiCheckInListItem
{
    public Guid Id { get; set; }
    public string KpiName { get; set; } = "";
    public string KpiCode { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateOnly CheckInDate { get; set; }
    public decimal ProgressValue { get; set; }
    public string ReviewStatus { get; set; } = "";
    public bool IsLate { get; set; }
    public decimal? ReviewScore { get; set; }
}

public class KpiCheckInSubmitViewModel
{
    [Required]
    public Guid KpiTargetId { get; set; }

    [Required]
    public decimal ProgressValue { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public class KpiCheckInReviewViewModel
{
    public Guid CheckInId { get; set; }

    [Required]
    public string Decision { get; set; } = "Approved"; // Approved or Rejected

    [StringLength(2000)]
    public string? Comment { get; set; }

    [Range(0, 100)]
    public decimal? Score { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Evaluation ViewModels
// ═══════════════════════════════════════════════════════════════════════════════

public class EvaluationListViewModel
{
    public List<EvaluationListItem> Items { get; set; } = new();
    public string? PeriodFilter { get; set; }
    public List<SelectOption> Periods { get; set; } = new();
}

public class EvaluationListItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string PeriodName { get; set; } = "";
    public decimal? TotalScore { get; set; }
    public string? RankName { get; set; }
    public string? Classification { get; set; }
    public string SubmissionStatus { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Mission/Vision ViewModels
// ═══════════════════════════════════════════════════════════════════════════════

public class MissionVisionListViewModel
{
    public List<MissionVisionItem> Items { get; set; } = new();
}

public class MissionVisionItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public int? TargetYear { get; set; }
    public string Content { get; set; } = "";
    public decimal? FinancialTarget { get; set; }
    public bool IsActive { get; set; }

    public string TypeDisplay => Type switch
    {
        "Vision" => "Tầm nhìn",
        "Mission" => "Sứ mệnh",
        "YearlyGoal" => "Mục tiêu chiến lược",
        _ => Type
    };
}

public class MissionVisionCreateViewModel
{
    [Required]
    public MissionVisionType Type { get; set; } = MissionVisionType.YearlyGoal;

    public int? TargetYear { get; set; }

    [Required(ErrorMessage = "Nội dung không được để trống")]
    [StringLength(4000)]
    public string? Content { get; set; }

    public decimal? FinancialTarget { get; set; }
}
