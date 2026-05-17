using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class GoodsIssueLine : TenantEntity
{
    public Guid GoodsIssueId { get; set; }
    public GoodsIssue? GoodsIssue { get; set; }

    public Guid? ProductServiceId { get; set; }
    public ProductService? ProductService { get; set; }

    [StringLength(250)]
    public string? ItemName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestedQuantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal IssuedQuantity { get; set; }

    [StringLength(50)]
    public string? UnitOfMeasure { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
