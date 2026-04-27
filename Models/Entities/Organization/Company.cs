namespace OmniBizAI.Models.Entities.Organization;

/// <summary>
/// Công ty — entity gốc của toàn bộ dữ liệu nghiệp vụ.
/// Bảng DB: companies (snake_case).
/// 
/// STUB: Entity tối giản cho BAO-02. Module Organization đầy đủ sẽ do An phát triển.
/// Các trường cơ bản (id, created_at, is_deleted) đã cấu hình chuẩn theo Blueprint mục 14.4.
/// </summary>
public class Company : SoftDeletableEntity
{
    /// <summary>Tên công ty (ví dụ: "OmniBiz Solutions")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mã công ty unique</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Mã số thuế</summary>
    public string? TaxCode { get; set; }

    /// <summary>Địa chỉ</summary>
    public string? Address { get; set; }

    /// <summary>Ngành nghề</summary>
    public string? Industry { get; set; }

    /// <summary>Tiền tệ mặc định</summary>
    public string Currency { get; set; } = "VND";

    /// <summary>Còn hoạt động</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Department> Departments { get; set; } = [];
}
