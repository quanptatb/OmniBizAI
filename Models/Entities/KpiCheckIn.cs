using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class KpiCheckIn
{
    public Guid Id { get; set; }

    public Guid KpiId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public decimal? PreviousValue { get; set; }

    public decimal NewValue { get; set; }

    public decimal? Progress { get; set; }

    public string Note { get; set; } = null!;

    public string EvidenceUrlsJson { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewComment { get; set; }

    public Guid? SubmittedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Kpi Kpi { get; set; } = null!;
}
