namespace OmniBizAI.Models.Entities.Finance;

/// <summary>
/// Represents a payment request submitted by staff for approval workflow.
/// Contains AI risk analysis results and links to line items.
/// </summary>
public class PaymentRequest : SoftDeletableEntity
{
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

    public string Priority { get; set; } = PaymentPriority.Normal.ToString();
    public string Status { get; set; } = PaymentRequestStatus.Draft.ToString();

    public decimal? AiRiskScore { get; set; }
    public string? AiRiskLevel { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Optimistic concurrency token (SQL Server rowversion).
    /// Required by Blueprint §14.14.3 for approve/reject conflict detection.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;

    // Navigation property
    public ICollection<PaymentRequestItem> Items { get; set; } = new List<PaymentRequestItem>();
}
