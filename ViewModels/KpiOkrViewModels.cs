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
    public List<Guid> SelectedEmployeeIds { get; set; } = new();

    // Dropdowns
    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Missions { get; set; } = new();
    public List<SelectOption> Employees { get; set; } = new();
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
    public Guid? OrganizationUnitId { get; set; }
    public Guid? OkrObjectiveId { get; set; }
    public Guid? OkrKeyResultId { get; set; }
    public Guid? EvaluationPeriodId { get; set; }
    public Guid? AssignerUserId { get; set; }
    public List<KpiTargetItem> Targets { get; set; } = new();
    public List<KpiDepartmentAssignmentItem> DepartmentAssignments { get; set; } = new();
    public List<KpiEmployeeAssignmentItem> EmployeeAssignments { get; set; } = new();
}

public class KpiDepartmentAssignmentItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
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
    public Guid? OwnerUserId { get; set; }
    public string? OwnerUserName { get; set; }
    public string? OwnerAvatarUrl { get; set; }
    public string? OwnerJobTitle { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DeadlineTimeDisplay { get; set; }
}

public class KpiEmployeeAssignmentItem
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public decimal Weight { get; set; }
    public string? AvatarUrl { get; set; }
    public string? JobTitle { get; set; }
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

    // Employee Selection
    public List<Guid> SelectedEmployeeIds { get; set; } = new();
    public List<EmployeeSelectOption> Employees { get; set; } = new();
}

public class EmployeeSelectOption
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
    public string DepartmentId { get; set; } = "";
}

public class MeetingSummaryImportViewModel
{
    [Required(ErrorMessage = "Tên cuộc họp không được để trống")]
    [StringLength(200)]
    public string MeetingTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập bản tóm tắt cuộc họp")]
    [StringLength(16000, MinimumLength = 50, ErrorMessage = "Bản tóm tắt cần ít nhất 50 ký tự để hệ thống phân tích")]
    public string SummaryContent { get; set; } = string.Empty;

    public bool AutoActivateOkr { get; set; }
    public bool AutoActivateKpis { get; set; } = true;

    public string DemoSummary { get; set; } = string.Empty;
    public string? PreviewPayloadJson { get; set; }
    public MeetingSummaryImportPreviewViewModel? Preview { get; set; }

    public List<SelectOption> Departments { get; set; } = new();
    public List<SelectOption> Missions { get; set; } = new();
    public List<SelectOption> Employees { get; set; } = new();
    public List<SelectOption> Periods { get; set; } = new();
}

public class MeetingSummaryImportPreviewViewModel
{
    public string MeetingTitle { get; set; } = string.Empty;
    public string SourceSummary { get; set; } = string.Empty;
    public string ParseMode { get; set; } = "RuleBased";
    public string Narrative { get; set; } = string.Empty;
    public string Cycle { get; set; } = string.Empty;
    public ImportedOkrDraftViewModel Okr { get; set; } = new();
    public List<ImportedKpiDraftViewModel> Kpis { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public int KeyResultCount => Okr.KeyResults.Count;
    public int KpiCount => Kpis.Count;
    public int DepartmentCount => Okr.DepartmentNames.Count;
    public int EmployeeCount => Okr.EmployeeNames.Count;
}

public class ImportedOkrDraftViewModel
{
    public string ObjectiveName { get; set; } = string.Empty;
    public OkrLevel Level { get; set; } = OkrLevel.Department;
    public string Cycle { get; set; } = string.Empty;

    public List<Guid> SelectedDepartmentIds { get; set; } = new();
    public List<string> DepartmentNames { get; set; } = new();

    public List<Guid> SelectedMissionIds { get; set; } = new();
    public List<string> MissionNames { get; set; } = new();

    public List<Guid> SelectedEmployeeIds { get; set; } = new();
    public List<string> EmployeeNames { get; set; } = new();

    public List<ImportedOkrKeyResultDraftViewModel> KeyResults { get; set; } = new();
}

public class ImportedOkrKeyResultDraftViewModel
{
    public int Index { get; set; }
    public string KeyResultName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal TargetValue { get; set; }
    public bool IsInverse { get; set; }
    public string? DepartmentName { get; set; }
}

public class ImportedKpiDraftViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public KpiOwnerType OwnerType { get; set; } = KpiOwnerType.Department;
    public KpiPeriodType PeriodType { get; set; } = KpiPeriodType.Monthly;
    public KpiMeasureType MeasureType { get; set; } = KpiMeasureType.Quantitative;
    public KpiPropertyType PropertyType { get; set; } = KpiPropertyType.Growth;

    public Guid? OrganizationUnitId { get; set; }
    public string? OrganizationUnitName { get; set; }

    public Guid? EvaluationPeriodId { get; set; }
    public string? EvaluationPeriodName { get; set; }

    public decimal TargetValue { get; set; }
    public decimal? PassThreshold { get; set; }
    public decimal? FailThreshold { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public int? CheckInFrequencyDays { get; set; }
    public TimeOnly? DeadlineTime { get; set; }
    public bool ReminderEnabled { get; set; } = true;

    public string? LinkedKeyResultName { get; set; }
    public int? LinkedKeyResultIndex { get; set; }
    public string? OwnerName { get; set; }
}

public class MeetingSummaryImportCommitViewModel
{
    [Required]
    public string PreviewPayloadJson { get; set; } = string.Empty;

    public bool AutoActivateOkr { get; set; }
    public bool AutoActivateKpis { get; set; }
}

public class MeetingSummaryImportCommitResult
{
    public Guid OkrId { get; set; }
    public string OkrName { get; set; } = string.Empty;
    public List<Guid> KpiIds { get; set; } = new();
    public Guid ImportJobId { get; set; }
    public string ParseMode { get; set; } = "RuleBased";
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
    // Stats
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int LateCount { get; set; }
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

public class KpiCheckInDetailViewModel
{
    public Guid Id { get; set; }
    public string KpiName { get; set; } = "";
    public string KpiCode { get; set; } = "";
    public Guid KpiTargetId { get; set; }
    public string UserName { get; set; } = "";
    public Guid UserId { get; set; }
    public string? SubmittedByName { get; set; }
    public DateOnly CheckInDate { get; set; }
    public decimal ProgressValue { get; set; }
    public string? Comment { get; set; }
    public string? FailReasonName { get; set; }
    public bool IsLate { get; set; }
    public DateTimeOffset? DeadlineAt { get; set; }
    public string ReviewStatus { get; set; } = "";
    public string? ReviewedByName { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
    public decimal? ReviewScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    // Detail items
    public List<KpiCheckInDetailLineItem> DetailItems { get; set; } = new();
    // History logs
    public List<KpiCheckInHistoryItem> HistoryLogs { get; set; } = new();
    // KPI target info
    public decimal? TargetValue { get; set; }
    public string? Unit { get; set; }
}

public class KpiCheckInDetailLineItem
{
    public Guid Id { get; set; }
    public string? MetricName { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? AchievedValue { get; set; }
    public string? Note { get; set; }
}

public class KpiCheckInHistoryItem
{
    public Guid Id { get; set; }
    public string Action { get; set; } = "";
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class KpiCheckInEditViewModel
{
    [Required] public Guid Id { get; set; }
    [Required] public decimal ProgressValue { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }
    public Guid? KpiFailReasonId { get; set; }
    public List<SelectOption> FailReasons { get; set; } = new();
    // Read-only display
    public string KpiName { get; set; } = "";
    public string KpiCode { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateOnly CheckInDate { get; set; }
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

// ═══════════════════════════════════════════════════════════════════════════════
// Enhancement VMs — OKR Edit, KPI Status, Evaluation Create, MV Edit, Dashboard
// ═══════════════════════════════════════════════════════════════════════════════

public class OkrEditViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Tên mục tiêu không được để trống")]
    [StringLength(255)]
    public string ObjectiveName { get; set; } = "";
    public OkrLevel Level { get; set; }
    [StringLength(50)]
    public string? Cycle { get; set; }
}

public class UpdateKrProgressViewModel
{
    public Guid KeyResultId { get; set; }
    public Guid OkrId { get; set; }
    [Required]
    public decimal CurrentValue { get; set; }
}

public class EvaluationCreateViewModel
{
    [Required]
    public Guid UserId { get; set; }
    [Required]
    public Guid EvaluationPeriodId { get; set; }
    [Range(0, 100)]
    public decimal? TotalScore { get; set; }
    [StringLength(100)]
    public string? Classification { get; set; }
    [StringLength(2000)]
    public string? Comment { get; set; }

    public List<SelectOption> Users { get; set; } = new();
    public List<SelectOption> Periods { get; set; } = new();
}

public class MissionVisionEditViewModel
{
    public Guid Id { get; set; }
    [Required]
    public MissionVisionType Type { get; set; }
    public int? TargetYear { get; set; }
    [Required(ErrorMessage = "Nội dung không được để trống")]
    [StringLength(4000)]
    public string? Content { get; set; }
    public decimal? FinancialTarget { get; set; }
}

public class KpiOkrDashboardViewModel
{
    public int TotalOkr { get; set; }
    public int ActiveOkr { get; set; }
    public int CompletedOkr { get; set; }
    public int TotalKpi { get; set; }
    public int ActiveKpi { get; set; }
    public int PendingCheckIns { get; set; }
    public int TotalEvaluations { get; set; }
    public decimal AvgOkrProgress { get; set; }
    public List<OkrListItem> RecentOkrs { get; set; } = new();
    public List<KpiFullListItem> RecentKpis { get; set; } = new();
}
