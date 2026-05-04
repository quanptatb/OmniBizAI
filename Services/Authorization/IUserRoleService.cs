// PR #23 — transaction boundary
namespace OmniBizAI.Services.Authorization;

/// <summary>
/// Checks whether a user has a specific RoleDefinition assigned in a tenant.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Returns true if the user has the specified role definition in the tenant.
    /// </summary>
    Task<bool> HasRoleAsync(
        string userId,
        Guid roleDefinitionId,
        Guid tenantId,
        CancellationToken ct = default);
}
