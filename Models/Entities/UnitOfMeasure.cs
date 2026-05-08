using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class UnitOfMeasure : TenantEntity
{
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<ProductService> ProductServices { get; set; } = new List<ProductService>();
}
