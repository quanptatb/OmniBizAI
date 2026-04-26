namespace OmniBizAI.Models.Entities.Finance;

/// <summary>
/// Represents a department/category budget for a specific fiscal period.
/// Tracks allocated, spent, committed amounts and utilization metrics.
/// </summary>
public class Budget : SoftDeletableEntity
{
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

    public string Status { get; set; } = BudgetStatus.Active.ToString();

    public string? Notes { get; set; }
}
