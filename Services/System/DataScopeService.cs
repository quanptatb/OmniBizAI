using OmniBizAI.Models.Common;
using OmniBizAI.Models.Entities.Finance;
using OmniBizAI.Models.Entities.Organization;

namespace OmniBizAI.Services.System;

/// <summary>
/// Stub implementation của IDataScopeService cho giai đoạn BAO-02.
/// 
/// HIỆN TẠI: Trả về IsAllScope = true (Admin/Director scope) để Budget service
/// hoạt động được mà không cần Auth module hoàn chỉnh.
/// 
/// SAU NÀY: An (module Auth/RBAC) sẽ implement đầy đủ logic resolve role, 
/// department hierarchy và permission matrix theo Blueprint mục 6.2.
/// </summary>
public sealed class DataScopeService : IDataScopeService
{
    /// <summary>
    /// STUB: Trả về scope toàn quyền (Admin/Director).
    /// TODO: An sẽ implement resolve từ Identity claims, Employee, Department.
    /// </summary>
    public Task<DataScope> GetScopeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var scope = new DataScope
        {
            UserId = userId,
            Role = "Admin",
            CompanyId = Guid.Empty, // Sẽ resolve từ Employee → Department → Company
            DepartmentId = null,
            IsAllScope = true, // Admin/Director xem toàn bộ
            AccessibleDepartmentIds = null
        };

        return Task.FromResult(scope);
    }

    /// <summary>
    /// Áp dụng data scope filter vào IQueryable.
    /// 
    /// Logic phân quyền dữ liệu:
    /// - IsAllScope = true (Admin/Director): không filter thêm → xem toàn công ty
    /// - Manager: filter theo danh sách DepartmentIds (phòng ban + phòng ban con)
    /// - Staff: filter theo cá nhân (requester_id, assignee_id, ...)
    /// 
    /// HIỆN TẠI: Chỉ áp dụng filter theo DepartmentId cho entity có property DepartmentId.
    /// Khi IsAllScope = true, không filter gì thêm.
    /// </summary>
    public IQueryable<T> ApplyScope<T>(IQueryable<T> query, DataScope scope) where T : class
    {
        // Admin/Director: xem toàn bộ dữ liệu công ty
        if (scope.IsAllScope)
        {
            return query;
        }

        // Manager: filter theo danh sách phòng ban được phép
        if (scope.AccessibleDepartmentIds is { Count: > 0 })
        {
            // Sử dụng reflection-free approach: kiểm tra type và cast
            if (typeof(T) == typeof(Budget))
            {
                var budgetQuery = query as IQueryable<Budget>;
                var departmentIds = scope.AccessibleDepartmentIds;
                return (IQueryable<T>)budgetQuery!.Where(b => departmentIds.Contains(b.DepartmentId));
            }

            if (typeof(T) == typeof(Department))
            {
                var deptQuery = query as IQueryable<Department>;
                var departmentIds = scope.AccessibleDepartmentIds;
                return (IQueryable<T>)deptQuery!.Where(d => departmentIds.Contains(d.Id));
            }
        }

        // Staff: filter theo phòng ban cá nhân
        if (scope.DepartmentId.HasValue)
        {
            if (typeof(T) == typeof(Budget))
            {
                var budgetQuery = query as IQueryable<Budget>;
                var deptId = scope.DepartmentId.Value;
                return (IQueryable<T>)budgetQuery!.Where(b => b.DepartmentId == deptId);
            }
        }

        return query;
    }

    /// <summary>
    /// STUB: Luôn trả true trong giai đoạn BAO-02.
    /// TODO: An sẽ implement kiểm tra entity ownership + permission.
    /// </summary>
    public Task<bool> CanAccessEntityAsync(Guid userId, string entityType, Guid entityId,
        string action, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
