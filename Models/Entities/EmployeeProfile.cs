using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniBizAI.Models.Entities;

public class EmployeeProfile
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
    
    public int DepartmentId { get; set; }
    public virtual Department Department { get; set; } = null!;
    
    [MaxLength(100)]
    public string? PositionName { get; set; }
    
    [MaxLength(20)]
    public string? EmployeeCode { get; set; }
    
    public DateTime? JoinDate { get; set; }
}
