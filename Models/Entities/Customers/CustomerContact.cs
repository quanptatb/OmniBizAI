// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Customers;

/// <summary>
/// Người liên hệ của khách hàng doanh nghiệp — nhận email duyệt.
/// </summary>
public sealed class CustomerContact
{
    public Guid Id { get; set; }
    public Guid CustomerCompanyId { get; set; }

    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(256)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? RoleTitle { get; set; }

    public bool IsPrimary { get; set; }
}
