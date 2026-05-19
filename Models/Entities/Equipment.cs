using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class Equipment : TenantEntity
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string Type { get; set; } = string.Empty; // Máy móc, Xe cộ, Thiết bị IT...

    [Required, StringLength(50)]
    public string Status { get; set; } = "Available"; // Available, InUse, Maintenance
}
