using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class EvaluationPeriod
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Status { get; set; } = null!;

    public decimal OkrWeight { get; set; }

    public decimal KpiWeight { get; set; }

    public string CheckInFrequency { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<Kpi> Kpis { get; set; } = new List<Kpi>();

    public virtual ICollection<Objective> Objectives { get; set; } = new List<Objective>();
}
