// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Menus;

namespace OmniBizAI.Models.Entities.Approvals;

/// <summary>
/// Token gửi cho khách hàng qua email để duyệt menu.
/// Lưu hash, không lưu token raw.
/// </summary>
public sealed class CustomerApprovalToken
{
    public Guid Id { get; set; }
    public Guid MenuPlanId { get; set; }
    public Guid CustomerContactId { get; set; }

    [MaxLength(256)]
    public string? SentToEmail { get; set; }

    /// <summary>
    /// SHA-256 hash of the token. Used for lookup.
    /// </summary>
    [MaxLength(128)]
    public string TokenHash { get; set; } = "";

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Status: pending, used, expired, revoked.
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public MenuPlan MenuPlan { get; set; } = null!;
}
