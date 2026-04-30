using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities;

public class Company
{
    [Key]
    public int Id { get; set; }
    
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(50)]
    public string? TaxCode { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
