// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Customers;

/// <summary>
/// Hợp đồng suất ăn với khách hàng — chứa default expected qty cho fallback.
/// </summary>
public sealed class ServiceContract
{
    public Guid Id { get; set; }
    public Guid CustomerCompanyId { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = "";

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid? DefaultMealShiftId { get; set; }
    public int? DefaultExpectedQty { get; set; }

    public bool IsActive { get; set; } = true;
}
