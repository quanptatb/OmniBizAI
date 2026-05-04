using Microsoft.Extensions.Options;
using OmniBizAI.Models.Auth;

namespace OmniBizAI.Services.Auth;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string roleCode, string permissionCode);
    Task<string?> GetRequiredPermissionAsync(string area, string controller, string action, string httpMethod);
}

public sealed class PermissionService : IPermissionService, IDisposable
{
    private readonly IOptionsMonitor<PermissionsConfig> configMonitor;
    private readonly IDisposable? reloadSubscription;
    private readonly object cacheLock = new();
    private PermissionCache? cache;

    public PermissionService(IOptionsMonitor<PermissionsConfig> configMonitor)
    {
        this.configMonitor = configMonitor;
        reloadSubscription = configMonitor.OnChange(_ =>
        {
            lock (cacheLock)
            {
                cache = null;
            }
        });
    }

    public Task<bool> HasPermissionAsync(string roleCode, string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode) || string.IsNullOrWhiteSpace(permissionCode))
        {
            return Task.FromResult(false);
        }

        var permissionCache = GetCache();
        var hasPerm = permissionCache.RolePermissions.TryGetValue(roleCode, out var permissions)
            && permissions.Contains(permissionCode);

        return Task.FromResult(hasPerm);
    }

    public Task<string?> GetRequiredPermissionAsync(string area, string controller, string action, string httpMethod)
    {
        var permissionCache = GetCache();
        var rule = permissionCache.RouteRules
            .Where(r => r.Matches(area, controller, action, httpMethod))
            .OrderByDescending(r => r.Specificity)
            .FirstOrDefault();

        return Task.FromResult(rule?.RequiredPermissionCode);
    }

    public void Dispose()
    {
        reloadSubscription?.Dispose();
    }

    private PermissionCache GetCache()
    {
        var currentCache = cache;
        if (currentCache != null)
        {
            return currentCache;
        }

        lock (cacheLock)
        {
            cache ??= PermissionCache.From(configMonitor.CurrentValue);
            return cache;
        }
    }

    private sealed class PermissionCache
    {
        public Dictionary<string, HashSet<string>> RolePermissions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CachedRouteRule> RouteRules { get; } = new();

        public static PermissionCache From(PermissionsConfig config)
        {
            var permissionCache = new PermissionCache();

            foreach (var role in config.RolePermissions)
            {
                if (string.IsNullOrWhiteSpace(role.RoleCode))
                {
                    continue;
                }

                permissionCache.RolePermissions[role.RoleCode] = role.Permissions
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var rule in config.RoutePermissionRules)
            {
                if (string.IsNullOrWhiteSpace(rule.Controller))
                {
                    continue;
                }

                permissionCache.RouteRules.Add(new CachedRouteRule(
                    rule.Area,
                    rule.Controller,
                    rule.Action,
                    rule.HttpMethod,
                    rule.RequiredPermissionCode));
            }

            return permissionCache;
        }
    }

    private sealed record CachedRouteRule(
        string Area,
        string Controller,
        string Action,
        string HttpMethod,
        string RequiredPermissionCode)
    {
        public int Specificity => SpecificityOf(Area)
            + SpecificityOf(Controller)
            + SpecificityOf(Action)
            + SpecificityOf(HttpMethod);

        public bool Matches(string area, string controller, string action, string httpMethod)
        {
            return MatchesField(Area, area)
                && MatchesField(Controller, controller)
                && MatchesField(Action, action)
                && MatchesField(HttpMethod, httpMethod);
        }

        private static int SpecificityOf(string pattern)
        {
            return string.Equals(pattern, "*", StringComparison.Ordinal) ? 0 : 1;
        }

        private static bool MatchesField(string? pattern, string? value)
        {
            pattern ??= string.Empty;
            value ??= string.Empty;

            return string.Equals(pattern, "*", StringComparison.Ordinal)
                || string.Equals(pattern, value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
