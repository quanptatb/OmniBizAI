using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class Department
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? ManagerId { get; set; }

    public decimal BudgetLimit { get; set; }

    public int Level { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Department> InverseParentDepartment { get; set; } = new List<Department>();

    public virtual ICollection<Kpi> Kpis { get; set; } = new List<Kpi>();

    public virtual Employee? Manager { get; set; }

    public virtual ICollection<Objective> Objectives { get; set; } = new List<Objective>();

    public virtual Department? ParentDepartment { get; set; }

    public virtual ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
