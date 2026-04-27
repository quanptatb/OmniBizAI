namespace OmniBizAI.Models.Common;

/// <summary>
/// Record chứa thông tin phạm vi dữ liệu của user hiện tại.
/// Blueprint mục 6.2: mỗi role có data scope khác nhau.
/// 
/// DataScope được resolve từ IDataScopeService và áp dụng vào mọi truy vấn nghiệp vụ
/// thông qua IDataScopeService.ApplyScope().
/// </summary>
public sealed record DataScope
{
    /// <summary>ID người dùng hiện tại</summary>
    public Guid UserId { get; init; }

    /// <summary>Role chính của user (Admin/Director/Manager/Accountant/HR/Staff)</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>ID phòng ban mà user thuộc về (null nếu Admin/Director xem toàn công ty)</summary>
    public Guid? DepartmentId { get; init; }

    /// <summary>ID công ty</summary>
    public Guid CompanyId { get; init; }

    /// <summary>
    /// True nếu user có quyền xem toàn bộ dữ liệu công ty (Admin, Director).
    /// False nếu chỉ xem theo phòng ban hoặc cá nhân (Manager, Staff, ...).
    /// </summary>
    public bool IsAllScope { get; init; }

    /// <summary>
    /// Danh sách ID phòng ban được phép xem (bao gồm phòng ban con nếu Manager).
    /// Null hoặc rỗng khi IsAllScope = true.
    /// </summary>
    public IReadOnlyList<Guid>? AccessibleDepartmentIds { get; init; }
}
