namespace OmniBizAI.Models.Entities;

public sealed class Vendor
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string Status { get; set; } = "Active";
    public decimal? Rating { get; set; }
    public DateTime CreatedAt { get; set; }

    public Company? Company { get; set; }
    public ICollection<PaymentRequest> PaymentRequests { get; set; } = [];
}
