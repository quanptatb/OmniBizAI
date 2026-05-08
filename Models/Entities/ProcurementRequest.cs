using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

public class ProcurementRequest : TenantEntity
{
    [StringLength(50)]
    public string RequestNo { get; set; } = string.Empty;

    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    public Guid RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    public ProcurementStatus Status { get; set; } = ProcurementStatus.Draft;

    public DateOnly? NeededByDate { get; set; }

    public ICollection<ProcurementRequestLine> Lines { get; set; } = new List<ProcurementRequestLine>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
