using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniBizAI.Models.Entities;

public class Department
{
    [Key]
    public int Id { get; set; }
    
    public int TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
    
    public int CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;
    
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public int? ParentDepartmentId { get; set; }
    [ForeignKey("ParentDepartmentId")]
    public virtual Department? ParentDepartment { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
