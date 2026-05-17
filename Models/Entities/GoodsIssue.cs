using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class GoodsIssue : TenantEntity
{
    [StringLength(50)]
    public string IssueNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string IssueType { get; set; } = "Internal"; // Internal, CustomerDelivery, Transfer, Adjustment

    public Guid? OperationRequestId { get; set; }
    public OperationRequest? OperationRequest { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid IssuedByUserId { get; set; }
    public AppUser? IssuedByUser { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public GoodsIssueStatus Status { get; set; } = GoodsIssueStatus.Draft;

    [StringLength(500)]
    public string? WarehouseLocation { get; set; }

    [StringLength(500)]
    public string? Destination { get; set; }

    [StringLength(2000)]
    public string? Note { get; set; }

    public ICollection<GoodsIssueLine> Lines { get; set; } = new List<GoodsIssueLine>();
}
