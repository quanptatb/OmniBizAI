using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class ProductService : TenantEntity
{
    public Guid? ProductCategoryId { get; set; }
    public ProductCategory? ProductCategory { get; set; }

    public Guid? UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = "Service";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? StandardPrice { get; set; }

    // Inventory management
    [Column(TypeName = "decimal(18,2)")]
    public decimal ReorderPoint { get; set; } = 0; // Điểm đặt hàng lại (mức tồn kho tối thiểu trước khi cần bổ sung)

    [Column(TypeName = "decimal(18,2)")]
    public decimal SafetyStock { get; set; } = 0; // Tồn kho an toàn (mức dự trữ bảo hiểm)

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxStock { get; set; } = 0; // Tồn kho tối đa

    public bool IsActive { get; set; } = true;
}
