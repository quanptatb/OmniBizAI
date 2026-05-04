using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.ViewModels;

public sealed class PaymentRequestItemRequest
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 99999999)]
    public decimal Quantity { get; set; } = 1;

    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = "Item";

    [Range(0, 999999999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 20)]
    public decimal TaxRate { get; set; }
}
