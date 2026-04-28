namespace OmniBizAI.Models.Entities;

public sealed class BudgetCategory
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Company? Company { get; set; }
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
}
