using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class ProcurementRequestLine : TenantEntity
{
    public Guid ProcurementRequestId { get; set; }
    public ProcurementRequest? ProcurementRequest { get; set; }

    public Guid? ProductServiceId { get; set; }
    public ProductService? ProductService { get; set; }

    [StringLength(250)]
    public string? ItemName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedUnitPrice { get; set; }
}
