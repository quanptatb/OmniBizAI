using Microsoft.AspNetCore.Identity;

namespace OmniBizAI.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public int? TenantId { get; set; }
    
    // Navigation property
    public virtual Tenant? Tenant { get; set; }

    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
