using OmniBizAI.Models.Common;

namespace OmniBizAI.Services.System;

/// <summary>
/// Interface phân quyền dữ liệu — Blueprint mục 3.6.
/// Mọi query list/detail/mutation phải đi qua ApplyScope() trước khi materialize.
/// 
/// RULE BẮT BUỘC (Blueprint mục 6.2):
/// - Không được viết trực tiếp _context.Budgets.ToListAsync() trong service/controller.
/// - Query detail phải filter scope trong cùng query.
/// - Code review phải reject PR nếu thấy query nghiệp vụ không áp scope.
/// </summary>
public interface IDataScopeService
{
    /// <summary>
    /// Lấy DataScope của user hiện tại (role, department, company, accessible departments).
    /// </summary>
    Task<DataScope> GetScopeAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Áp dụng data scope filter vào IQueryable.
    /// Phải gọi method này TRƯỚC khi ToListAsync/FirstOrDefaultAsync/CountAsync.
    /// </summary>
    IQueryable<T> ApplyScope<T>(IQueryable<T> query, DataScope scope) where T : class;

    /// <summary>
    /// Kiểm tra user có quyền truy cập entity cụ thể không.
    /// </summary>
    Task<bool> CanAccessEntityAsync(Guid userId, string entityType, Guid entityId,
        string action, CancellationToken cancellationToken = default);
}
