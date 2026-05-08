using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class Budget : TenantEntity
{
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public int FiscalYear { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PlannedAmount { get; set; }

    public BudgetStatus Status { get; set; } = BudgetStatus.Draft;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
