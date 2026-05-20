using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class ProductTraceability : TenantEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public Guid ProductServiceId { get; set; }
    public ProductService? ProductService { get; set; }

    [Required, StringLength(100)]
    public string BatchNo { get; set; } = string.Empty;

    [Required]
    public string OriginDetails { get; set; } = string.Empty; // Lưu thông tin nhật ký dạng JSON / RichText

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
