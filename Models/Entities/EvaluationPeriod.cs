using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class EvaluationPeriod : TenantEntity
{
    [StringLength(100)]
    public string PeriodName { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public EvaluationPeriodStatus Status { get; set; } = EvaluationPeriodStatus.Open;

    [StringLength(500)]
    public string? Description { get; set; }
}
