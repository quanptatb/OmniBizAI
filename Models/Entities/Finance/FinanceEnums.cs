namespace OmniBizAI.Models.Entities.Finance;

/// <summary>
/// Enum trạng thái ngân sách.
/// Blueprint mục 14.4.3: Active/Frozen/Closed.
/// </summary>
public enum BudgetStatus
{
    /// <summary>Ngân sách đang hoạt động, cho phép tạo PR</summary>
    Active,

    /// <summary>Ngân sách tạm đóng, hạn chế tạo PR mới</summary>
    Frozen,

    /// <summary>Ngân sách đã đóng sổ, không cho phép giao dịch mới</summary>
    Closed
}

/// <summary>
/// Mức cảnh báo utilization ngân sách.
/// Dùng trong BudgetUtilizationDto để UI hiển thị badge màu.
/// </summary>
public enum WarningLevel
{
    /// <summary>Bình thường (utilization &lt; 80%)</summary>
    Normal,

    /// <summary>Cảnh báo vàng (80% &lt;= utilization &lt;= 100%)</summary>
    Warning,

    /// <summary>Cảnh báo đỏ — vượt ngân sách (utilization &gt; 100%)</summary>
    Critical
}
