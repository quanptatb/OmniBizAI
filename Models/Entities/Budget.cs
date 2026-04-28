namespace OmniBizAI.Models.Entities;

public sealed class Budget
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid FiscalPeriodId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AllocatedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal UtilizationPct { get; set; }
    public decimal WarningThreshold { get; set; } = 80;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    public Company? Company { get; set; }
    public Department? Department { get; set; }
    public BudgetCategory? Category { get; set; }
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
}
