using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class CustomerContact : TenantEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public bool IsPrimary { get; set; }
}
