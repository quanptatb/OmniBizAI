using OmniBizAI.Domain.Common;
using OmniBizAI.Domain.Entities.Identity;

namespace OmniBizAI.Domain.Entities.Organization;

public sealed class Company : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string DefaultCurrency { get; set; } = "VND";
    public int FiscalYearStartMonth { get; set; } = 1;
    public string SettingsJson { get; set; } = "{}";
    public ICollection<Department> Departments { get; set; } = new List<Department>();
}

public sealed class Department : SoftDeletableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public decimal BudgetLimit { get; set; }
    public int Level { get; set; } = 1;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Department> Children { get; set; } = new List<Department>();
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public sealed class Position : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Employee : SoftDeletableEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? PositionId { get; set; }
    public Position? Position { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? LeaveDate { get; set; }
    public string EmploymentType { get; set; } = "FullTime";
    public string Status { get; set; } = "Active";
    public string? AvatarUrl { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
}

public sealed class EmployeeHistory : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string ChangeType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Reason { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
