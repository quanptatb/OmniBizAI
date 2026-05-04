// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Configuration;

/// <summary>
/// Đồ thị chuyển trạng thái cấu hình theo tenant.
/// Engine đọc bảng này để validate transition.
/// </summary>
public sealed class StateTransitionConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>
    /// State machine name, e.g. "menu_plan", "procurement", "approval".
    /// </summary>
    [MaxLength(100)]
    public string StateMachine { get; set; } = "";

    [MaxLength(50)]
    public string FromStateCode { get; set; } = "";

    /// <summary>
    /// Action code that triggers the transition, e.g. "approve", "request_change", "issue".
    /// </summary>
    [MaxLength(100)]
    public string ActionCode { get; set; } = "";

    [MaxLength(50)]
    public string ToStateCode { get; set; } = "";

    [MaxLength(100)]
    public string? RequiredPermissionCode { get; set; }

    /// <summary>
    /// Optional JSON guard rules (for future extension).
    /// </summary>
    public string? GuardRuleJson { get; set; }

    public bool IsActive { get; set; } = true;
}
