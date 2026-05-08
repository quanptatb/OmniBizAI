using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class PaymentRequestLine : TenantEntity
{
    public Guid PaymentRequestId { get; set; }
    public PaymentRequest? PaymentRequest { get; set; }

    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(80)]
    public string? CostCategory { get; set; }
}
