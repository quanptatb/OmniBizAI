// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Menus;

/// <summary>
/// Lưu lịch sử revision khi menu bị yêu cầu chỉnh sửa.
/// </summary>
public sealed class MenuPlanRevision
{
    public Guid Id { get; set; }
    public Guid MenuPlanId { get; set; }
    public int RevisionNo { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// JSON snapshot of the menu plan at this revision.
    /// </summary>
    public string SnapshotJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public MenuPlan MenuPlan { get; set; } = null!;
}
