using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class ProductCategory : TenantEntity
{
    public Guid? ParentId { get; set; }
    public ProductCategory? Parent { get; set; }

    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();
    public ICollection<ProductService> ProductServices { get; set; } = new List<ProductService>();
}
