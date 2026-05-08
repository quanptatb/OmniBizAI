using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class Expense : TenantEntity
{
    public Guid? BudgetId { get; set; }
    public Budget? Budget { get; set; }

    public Guid? PaymentRequestId { get; set; }
    public PaymentRequest? PaymentRequest { get; set; }

    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public ExpenseStatus Status { get; set; } = ExpenseStatus.Recorded;
}
