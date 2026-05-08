using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class OperationRequest : TenantEntity
{
    [StringLength(50)]
    public string RequestNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? CustomerSiteId { get; set; }
    public CustomerSite? CustomerSite { get; set; }

    public Guid OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public Guid RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;

    public OperationStatus Status { get; set; } = OperationStatus.Draft;

    public DateOnly? DueDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAmount { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public ICollection<OperationRequestLine> Lines { get; set; } = new List<OperationRequestLine>();
    public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    public ICollection<AiInsight> AiInsights { get; set; } = new List<AiInsight>();
}
