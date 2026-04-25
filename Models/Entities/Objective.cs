using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class Objective
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid PeriodId { get; set; }

    public Guid? ParentId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string OwnerType { get; set; } = null!;

    public Guid? DepartmentId { get; set; }

    public Guid? OwnerId { get; set; }

    public decimal Progress { get; set; }

    public string Status { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Department? Department { get; set; }

    public virtual ICollection<Objective> InverseParent { get; set; } = new List<Objective>();

    public virtual ICollection<KeyResult> KeyResults { get; set; } = new List<KeyResult>();

    public virtual Employee? Owner { get; set; }

    public virtual Objective? Parent { get; set; }

    public virtual EvaluationPeriod Period { get; set; } = null!;
}
