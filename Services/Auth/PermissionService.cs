using Microsoft.Extensions.Options;
using OmniBizAI.Models.Auth;

namespace OmniBizAI.Services.Auth;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string roleCode, string permissionCode);
    Task<string?> GetRequiredPermissionAsync(string area, string controller, string action, string httpMethod);
}

public class PermissionService(IOptionsMonitor<PermissionsConfig> configMonitor) : IPermissionService
{
    public Task<bool> HasPermissionAsync(string roleCode, string permissionCode)
    {
        var config = configMonitor.CurrentValue;
        var roleConfig = config.RolePermissions.FirstOrDefault(r => r.RoleCode.Equals(roleCode, StringComparison.OrdinalIgnoreCase));
        
        if (roleConfig == null)
        {
            return Task.FromResult(false);
        }

        var hasPerm = roleConfig.Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(hasPerm);
    }

    public Task<string?> GetRequiredPermissionAsync(string area, string controller, string action, string httpMethod)
    {
        var config = configMonitor.CurrentValue;
        
        // Exact match
        var rule = config.RoutePermissionRules.FirstOrDefault(r => 
            string.Equals(r.Area, area, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.Action, action, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase));

        // Wildcard fallback (e.g. Action = "*")
        if (rule == null)
        {
            rule = config.RoutePermissionRules.FirstOrDefault(r => 
                string.Equals(r.Area, area, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
                r.Action == "*" &&
                (string.Equals(r.HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase) || r.HttpMethod == "*"));
        }

        return Task.FromResult(rule?.RequiredPermissionCode);
    }
}
