using System;
using System.Collections.Generic;

namespace OmniBizAI.Models.Entities;

public partial class PaymentRequestItem
{
    public Guid Id { get; set; }

    public Guid PaymentRequestId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string? Unit { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public int SortOrder { get; set; }

    public virtual PaymentRequest PaymentRequest { get; set; } = null!;
}
