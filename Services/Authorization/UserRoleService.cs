// PR #23 — transaction boundary
using Microsoft.EntityFrameworkCore;
using OmniBizAI.Data;

namespace OmniBizAI.Services.Authorization;

/// <summary>
/// Reads UserRoleAssignment table to check if a user has a specific role in a tenant.
/// </summary>
public sealed class UserRoleService : IUserRoleService
{
    private readonly ApplicationDbContext _db;

    public UserRoleService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasRoleAsync(
        string userId,
        Guid roleDefinitionId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _db.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(ura =>
                ura.TenantId == tenantId &&
                ura.UserId == userId &&
                ura.RoleDefinitionId == roleDefinitionId &&
                ura.IsActive,
                ct);
    }
}
