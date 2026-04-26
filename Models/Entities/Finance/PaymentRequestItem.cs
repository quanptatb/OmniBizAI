namespace OmniBizAI.Models.Entities.Finance;

/// <summary>
/// Represents a single line item within a payment request.
/// Defines its own Id (not inheriting SoftDeletableEntity) and is
/// cascade-deleted when its parent PaymentRequest is removed.
/// </summary>
public class PaymentRequestItem
{
    public Guid Id { get; set; }

    public Guid PaymentRequestId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "Item";
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    public int SortOrder { get; set; }

    // Navigation property
    public PaymentRequest PaymentRequest { get; set; } = null!;
}
