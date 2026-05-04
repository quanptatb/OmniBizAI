// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Menus;

namespace OmniBizAI.Models.Entities.Approvals;

/// <summary>
/// Bước duyệt nội bộ cho một MenuPlan. Mỗi workflow step tạo một record.
/// </summary>
public sealed class InternalApproval
{
    public Guid Id { get; set; }
    public Guid MenuPlanId { get; set; }

    /// <summary>
    /// Thứ tự bước duyệt trong workflow (1-based).
    /// </summary>
    public int SequenceNo { get; set; }

    [MaxLength(100)]
    public string StepName { get; set; } = "";

    /// <summary>
    /// FK to RoleDefinition — the role required to decide this step.
    /// </summary>
    public Guid RequiredRoleDefinitionId { get; set; }

    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Status code: pending, approved, change_requested, cancelled.
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Optional — the specific user assigned to this step (null = any user with the role).
    /// </summary>
    [MaxLength(450)]
    public string? AssignedToUserId { get; set; }

    [MaxLength(450)]
    public string? DecidedByUserId { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public MenuPlan MenuPlan { get; set; } = null!;
}
