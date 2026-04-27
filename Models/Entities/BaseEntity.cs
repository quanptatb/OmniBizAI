namespace OmniBizAI.Models.Entities;

/// <summary>
/// Entity gốc cho toàn bộ hệ thống — chứa khóa chính dạng GUID.
/// Mọi entity nghiệp vụ phải kế thừa từ class này hoặc các class con.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

/// <summary>
/// Entity có audit trail — ghi nhận thời gian và người tạo/cập nhật.
/// Dùng cho các entity cần truy vết nhưng KHÔNG cần soft delete.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>Thời gian tạo (UTC)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Thời gian cập nhật gần nhất (UTC)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>User tạo record</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>User cập nhật gần nhất</summary>
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Entity hỗ trợ xóa mềm (soft delete) — không xóa vĩnh viễn khỏi DB.
/// Dùng cho các entity nghiệp vụ chính: Budget, PaymentRequest, Employee, Department, ...
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    /// <summary>Đánh dấu đã xóa mềm</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Thời gian xóa mềm (UTC)</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Concurrency token — phục vụ optimistic concurrency</summary>
    public byte[] RowVersion { get; set; } = [];
}
