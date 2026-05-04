// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Approvals;

/// <summary>
/// Timeline hiển thị cho người dùng — ghi lại mỗi hành động duyệt/yêu cầu sửa.
/// </summary>
public sealed class ApprovalTimeline
{
    public Guid Id { get; set; }

    /// <summary>
    /// Entity type this timeline belongs to, e.g. "MenuPlan", "ProcurementPlan".
    /// </summary>
    [MaxLength(100)]
    public string EntityType { get; set; } = "";

    public Guid EntityId { get; set; }

    /// <summary>
    /// Action code, e.g. "approve", "request_change", "internal_approve", "submit_internal".
    /// </summary>
    [MaxLength(100)]
    public string Action { get; set; } = "";

    /// <summary>
    /// Actor type: "Internal", "Customer", "System".
    /// </summary>
    [MaxLength(50)]
    public string ActorType { get; set; } = "";

    [MaxLength(200)]
    public string ActorName { get; set; } = "";

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
