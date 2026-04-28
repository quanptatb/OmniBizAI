namespace OmniBizAI.Models.Entities;

public sealed class Department
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Company? Company { get; set; }
    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
}
