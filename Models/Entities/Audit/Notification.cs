// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Audit;

/// <summary>
/// Thông báo nội bộ cho user — e.g. "Cần nhập số lượng thủ công".
/// </summary>
public sealed class Notification
{
    public Guid Id { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; } = "";

    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(2000)]
    public string Message { get; set; } = "";

    [MaxLength(100)]
    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
