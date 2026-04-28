namespace OmniBizAI.Models.Entities;

public sealed class PaymentRequest
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? BudgetId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Priority { get; set; } = "Normal";
    public PaymentRequestStatus Status { get; set; } = PaymentRequestStatus.Draft;
    public decimal? AiRiskScore { get; set; }
    public string? AiRiskLevel { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Company? Company { get; set; }
    public Department? Department { get; set; }
    public Employee? Requester { get; set; }
    public Vendor? Vendor { get; set; }
    public Budget? Budget { get; set; }
    public BudgetCategory? Category { get; set; }
    public ICollection<PaymentRequestItem> Items { get; set; } = [];
}
