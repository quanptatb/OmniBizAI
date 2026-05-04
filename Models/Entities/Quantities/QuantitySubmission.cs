// PR #23 — transaction boundary
using System.ComponentModel.DataAnnotations;

namespace OmniBizAI.Models.Entities.Quantities;

/// <summary>
/// Lịch sử mỗi lần nhập số lượng — từ khách hoặc nội bộ.
/// </summary>
public sealed class QuantitySubmission
{
    public Guid Id { get; set; }
    public Guid DailyMealOrderId { get; set; }

    [MaxLength(200)]
    public string SubmittedByName { get; set; } = "";

    [MaxLength(256)]
    public string? SubmittedByEmail { get; set; }

    /// <summary>
    /// Source code: customer_email, internal_manual, import, system_fallback.
    /// </summary>
    [MaxLength(50)]
    public string SourceCode { get; set; } = "";

    public int? ExpectedQtyInput { get; set; }
    public int? FinalQtyInput { get; set; }
    public int? ExtraQtyInput { get; set; }

    [MaxLength(50)]
    public string? ExtraModeCode { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public DailyMealOrder DailyMealOrder { get; set; } = null!;
}
