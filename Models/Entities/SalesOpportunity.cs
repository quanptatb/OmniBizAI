using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

/// <summary>Sales opportunity / deal tracking through pipeline stages.</summary>
public class SalesOpportunity : TenantEntity
{
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? CustomerContactId { get; set; }
    public CustomerContact? CustomerContact { get; set; }

    /// <summary>Lead, Qualified, Proposal, Negotiation, ClosedWon, ClosedLost</summary>
    [StringLength(50)]
    public string Stage { get; set; } = "Lead";

    /// <summary>Estimated deal value</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedValue { get; set; }

    /// <summary>0–100 win probability</summary>
    public int Probability { get; set; } = 10;

    /// <summary>Hot, Warm, Cold</summary>
    [StringLength(30)]
    public string Temperature { get; set; } = "Warm";

    [StringLength(250)]
    public string? Source { get; set; }

    public DateOnly? ExpectedCloseDate { get; set; }

    public DateOnly? ActualCloseDate { get; set; }

    [StringLength(2000)]
    public string? LostReason { get; set; }

    [StringLength(2000)]
    public string? WonNote { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    /// <summary>Associated products/services as JSON or line items</summary>
    [StringLength(4000)]
    public string? ProductsJson { get; set; }
}
