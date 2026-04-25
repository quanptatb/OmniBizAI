using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class Employee
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid CompanyId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? PositionId { get; set; }

    public Guid? ManagerId { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly JoinDate { get; set; }

    public DateOnly? LeaveDate { get; set; }

    public string EmploymentType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? BankAccount { get; set; }

    public string? BankName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Department? Department { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<EmployeeHistory> EmployeeHistories { get; set; } = new List<EmployeeHistory>();

    public virtual ICollection<Employee> InverseManager { get; set; } = new List<Employee>();

    public virtual ICollection<KeyResult> KeyResults { get; set; } = new List<KeyResult>();

    public virtual ICollection<Kpi> Kpis { get; set; } = new List<Kpi>();

    public virtual Employee? Manager { get; set; }

    public virtual ICollection<Objective> Objectives { get; set; } = new List<Objective>();

    public virtual ICollection<PaymentRequest> PaymentRequests { get; set; } = new List<PaymentRequest>();

    public virtual ICollection<PerformanceEvaluation> PerformanceEvaluations { get; set; } = new List<PerformanceEvaluation>();

    public virtual Position? Position { get; set; }

    public virtual User? User { get; set; }
}
