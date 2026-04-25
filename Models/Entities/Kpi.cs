using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class Kpi
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid PeriodId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? AssigneeId { get; set; }

    public string MetricType { get; set; } = null!;

    public string? Unit { get; set; }

    public decimal StartValue { get; set; }

    public decimal TargetValue { get; set; }

    public decimal CurrentValue { get; set; }

    public decimal Weight { get; set; }

    public decimal Progress { get; set; }

    public string Frequency { get; set; } = null!;

    public string? Formula { get; set; }

    public string Direction { get; set; } = null!;

    public string Status { get; set; } = null!;

    public decimal? Score { get; set; }

    public string? Rating { get; set; }

    public DateTime? LastCheckInAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Employee? Assignee { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Department? Department { get; set; }

    public virtual ICollection<KpiCheckIn> KpiCheckIns { get; set; } = new List<KpiCheckIn>();

    public virtual EvaluationPeriod Period { get; set; } = null!;
}
