using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.ViewModels.Kpi;

public class CreateObjectiveRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public Guid EvaluationPeriodId { get; set; }
}

public class ObjectiveDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Progress { get; set; }
    public Guid EvaluationPeriodId { get; set; }
}

public class CreateKeyResultRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public Guid ObjectiveId { get; set; }
    public decimal TargetValue { get; set; }
}

public class KeyResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Progress { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid ObjectiveId { get; set; }
}

public class CreateKpiRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public string Direction { get; set; } = "IncreaseIsBetter";
    public Guid EvaluationPeriodId { get; set; }
    public Guid OwnerId { get; set; }
}

public class KpiDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Progress { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid EvaluationPeriodId { get; set; }
    public Guid OwnerId { get; set; }
}

public class SubmitCheckInRequest
{
    public Guid KpiId { get; set; }
    public decimal NewValue { get; set; }
    public string? Comment { get; set; }
}

public class ReviewCheckInRequest
{
    public string? Comment { get; set; }
}

public class CheckInDto
{
    public Guid Id { get; set; }
    public Guid KpiId { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal NewValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmployeeScorecardDto
{
    public Guid EmployeeId { get; set; }
    public Guid EvaluationPeriodId { get; set; }
    public decimal OverallProgress { get; set; }
    public List<KpiDto> Kpis { get; set; } = new();
}
