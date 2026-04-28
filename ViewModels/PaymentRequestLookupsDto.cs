namespace OmniBizAI.ViewModels;

public sealed class PaymentRequestLookupsDto
{
    public IReadOnlyList<LookupItemDto> Departments { get; init; } = [];
    public IReadOnlyList<LookupItemDto> Categories { get; init; } = [];
    public IReadOnlyList<LookupItemDto> Vendors { get; init; } = [];
    public IReadOnlyList<LookupItemDto> Budgets { get; init; } = [];
}

public sealed class LookupItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
