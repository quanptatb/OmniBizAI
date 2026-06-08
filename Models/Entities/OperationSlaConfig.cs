using System.ComponentModel.DataAnnotations;
using OmniBizAI.Models.Entities.Common;
using OmniBizAI.Models.Entities.Enums;

namespace OmniBizAI.Models.Entities;

/// <summary>
/// Cấu hình SLA cho OperationRequest theo Priority và (tùy chọn) Department.
/// Khi tạo request, hệ thống tự tính DueDate = now + ResolveHours nếu user không nhập.
/// </summary>
public class OperationSlaConfig : TenantEntity
{
    public PriorityLevel Priority { get; set; }

    /// <summary>Null = áp dụng cho mọi phòng ban.</summary>
    public Guid? OrganizationUnitId { get; set; }
    public OrganizationUnit? OrganizationUnit { get; set; }

    /// <summary>Thời gian phản hồi tối đa (giờ) — manager phải Approve/StartWork trong khoảng này.</summary>
    public int ResponseHours { get; set; } = 8;

    /// <summary>Thời gian xử lý tối đa (giờ) — yêu cầu phải Complete trong khoảng này từ lúc Submit.</summary>
    public int ResolveHours { get; set; } = 48;

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Description { get; set; }
}
