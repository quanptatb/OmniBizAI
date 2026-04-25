using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class PerformanceEvaluation
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid PeriodId { get; set; }

    public Guid EmployeeId { get; set; }

    public Guid? DepartmentId { get; set; }

    public decimal? OkrScore { get; set; }

    public decimal? KpiScore { get; set; }

    public decimal? TotalScore { get; set; }

    public string? Rating { get; set; }

    public string? ManagerComment { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
