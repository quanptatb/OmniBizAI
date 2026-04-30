using Microsoft.AspNetCore.Identity;

namespace OmniBizAI.Models.Entities;

public class ApplicationRole : IdentityRole
{
    public int? TenantId { get; set; }
    public string Description { get; set; } = string.Empty;
}
