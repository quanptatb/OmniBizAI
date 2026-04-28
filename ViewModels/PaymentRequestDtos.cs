namespace OmniBizAI.ViewModels;

public sealed class PaymentRequestDetailDto
{
    public Guid Id { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public Guid? VendorId { get; init; }
    public string? VendorName { get; init; }
    public Guid? BudgetId { get; init; }
    public string? BudgetName { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public DateOnly? PaymentDueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "VND";
    public string Priority { get; init; } = "Normal";
    public string Status { get; init; } = "Draft";
    public IReadOnlyList<PaymentRequestItemDto> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
}

public sealed class PaymentRequestItemDto
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = "Item";
    public decimal UnitPrice { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalPrice { get; init; }
}

public sealed class PaymentRequestListItemDto
{
    public Guid Id { get; init; }
    public string RequestNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string RequesterName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Priority { get; init; } = "Normal";
    public string Status { get; init; } = "Draft";
    public string RiskLevel { get; init; } = "Low";
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
}

public sealed class PaymentRequestFilter
{
    public string? Status { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? RequesterId { get; init; }
    public Guid? VendorId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}
