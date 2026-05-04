// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Procurement;

/// <summary>
/// Giấy đi chợ tổng — gom nhiều menu cùng ngày/ca.
/// </summary>
public sealed class ProcurementPlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = "";

    public DateOnly ServiceDate { get; set; }
    public Guid MealShiftId { get; set; }

    /// <summary>
    /// Status code: draft, issued, cancelled.
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset? GeneratedAt { get; set; }

    [MaxLength(450)]
    public string? GeneratedByUserId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public ICollection<ProcurementPlanMenu> ProcurementPlanMenus { get; set; } = [];
    public ICollection<ProcurementPlanLine> Lines { get; set; } = [];
}
