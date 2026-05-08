using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class OperationRequestLine : TenantEntity
{
    public Guid OperationRequestId { get; set; }
    public OperationRequest? OperationRequest { get; set; }

    public Guid? ProductServiceId { get; set; }
    public ProductService? ProductService { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LineAmount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
