namespace OmniBizAI.ViewModels;

public sealed class CreatePaymentRequestRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid? VendorId { get; init; }
    public Guid? BudgetId { get; init; }
    public DateOnly? PaymentDueDate { get; init; }
    public string Priority { get; init; } = "Normal";
    public IReadOnlyList<PaymentRequestItemRequest> Items { get; init; } = [];
}
