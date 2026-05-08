using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;

namespace OmniBizAI.Models.Entities;

public class Customer : TenantEntity
{
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? TaxCode { get; set; }

    [StringLength(100)]
    public string? Industry { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
    public ICollection<CustomerSite> Sites { get; set; } = new List<CustomerSite>();
    public ICollection<OperationRequest> OperationRequests { get; set; } = new List<OperationRequest>();
}
