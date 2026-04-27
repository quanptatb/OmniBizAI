using OmniBizAI.Models.Common;
using OmniBizAI.ViewModels.Finance;

namespace OmniBizAI.Services.Finance;

/// <summary>
/// Service interface quản lý ngân sách.
/// Blueprint mục 7.3: IBudgetService sở hữu toàn bộ logic ngân sách.
/// 
/// Quy tắc bắt buộc:
/// - Mọi truy vấn PHẢI áp dụng Data Scope (IDataScopeService.ApplyScope).
/// - Chỉ Finance service được thay đổi spent_amount, committed_amount, remaining_amount.
/// - Controller không chứa logic tính toán hay truy vấn EF Core trực tiếp.
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// Lấy danh sách ngân sách có phân trang, filter, search và data scope.
    /// </summary>
    /// <param name="filter">Bộ lọc + phân trang</param>
    /// <param name="userId">ID user hiện tại (để resolve data scope)</param>
    /// <param name="cancellationToken">Token hủy</param>
    /// <returns>Danh sách BudgetDto phân trang</returns>
    Task<PagedResult<BudgetDto>> GetBudgetsAsync(BudgetFilter filter, Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tính toán và trả về thông tin utilization của một ngân sách cụ thể.
    /// Dùng cho JSON endpoint UtilizationJson.
    /// </summary>
    /// <param name="budgetId">ID ngân sách</param>
    /// <param name="userId">ID user hiện tại (để kiểm tra data scope)</param>
    /// <param name="cancellationToken">Token hủy</param>
    /// <returns>BudgetUtilizationDto với công thức tính toán và warning level</returns>
    Task<BudgetUtilizationDto> GetUtilizationAsync(Guid budgetId, Guid userId,
        CancellationToken cancellationToken = default);
}
