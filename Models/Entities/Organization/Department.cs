namespace OmniBizAI.Models.Entities.Organization;

/// <summary>
/// Phòng ban — đơn vị tổ chức, dùng cho data scope và phân bổ ngân sách.
/// Bảng DB: departments (snake_case).
/// 
/// STUB: Entity tối giản cho BAO-02. Module Organization đầy đủ sẽ do An phát triển.
/// Hỗ trợ cây cha-con (ParentDepartmentId) theo Blueprint mục 7.2.
/// </summary>
public class Department : SoftDeletableEntity
{
    /// <summary>FK tới Company</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Mã phòng ban unique (ví dụ: "MKT", "FIN", "IT")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên phòng ban (ví dụ: "Marketing")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>FK phòng ban cha (hỗ trợ cây tổ chức)</summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>Giới hạn ngân sách phòng ban</summary>
    public decimal BudgetLimit { get; set; }

    /// <summary>Còn hoạt động</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Thứ tự hiển thị</summary>
    public int SortOrder { get; set; }

    // Navigation
    public Company Company { get; set; } = null!;
    public Department? ParentDepartment { get; set; }
    public virtual ICollection<Department> ChildDepartments { get; set; } = [];
}
