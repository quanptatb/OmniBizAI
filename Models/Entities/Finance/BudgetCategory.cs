namespace OmniBizAI.Models.Entities.Finance;

/// <summary>
/// Danh mục ngân sách — phân loại chi phí (Marketing Ads, Office Supplies, ...).
/// Bảng DB: budget_categories (snake_case).
/// Blueprint mục 5.2 và 9.4: 18 danh mục.
/// </summary>
public class BudgetCategory : AuditableEntity
{
    /// <summary>Mã danh mục unique (ví dụ: "MARKETING_ADS")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên hiển thị (ví dụ: "Chi phí quảng cáo Marketing")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả</summary>
    public string? Description { get; set; }

    /// <summary>FK danh mục cha (hỗ trợ cấu trúc tree)</summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>Còn sử dụng hay không</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Thứ tự hiển thị</summary>
    public int SortOrder { get; set; }

    // Navigation
    public BudgetCategory? ParentCategory { get; set; }
    public virtual ICollection<BudgetCategory> ChildCategories { get; set; } = [];
    public virtual ICollection<Budget> Budgets { get; set; } = [];
}
