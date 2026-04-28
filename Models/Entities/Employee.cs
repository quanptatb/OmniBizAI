namespace OmniBizAI.Models.Entities;

public sealed class Employee
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string EmploymentType { get; set; } = "FullTime";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    public Company? Company { get; set; }
    public Department? Department { get; set; }
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
}
