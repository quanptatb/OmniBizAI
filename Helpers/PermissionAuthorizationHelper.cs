namespace OmniBizAI.Helpers
{
    /// <summary>
    /// Provides permission authorization logic including legacy alias expansion
    /// and role-based default permissions.
    /// </summary>
    public static class PermissionAuthorizationHelper
    {
        private static readonly Dictionary<string, string[]> LegacyPermissionAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BONUSRULES_VIEW"] = new[] { "BONUS_VIEW" },
            ["BONUSRULES_CREATE"] = new[] { "BONUS_EDIT" },
            ["BONUSRULES_EDIT"] = new[] { "BONUS_EDIT" },
            ["BONUSRULES_DELETE"] = new[] { "BONUS_EDIT" }
        };

        private static readonly HashSet<string> HrDefaultPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            "EMPLOYEES_VIEW",
            "EVALPERIODS_VIEW",
            "BONUSRULES_VIEW"
        };

        public static bool IsHrRole(IEnumerable<string> roleNames)
        {
            return roleNames.Any(role =>
                string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Human Resources", StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasRoleDefaultPermission(IEnumerable<string> roleNames, IEnumerable<string> requestedPermissions)
        {
            return IsHrRole(roleNames) && requestedPermissions.Any(HrDefaultPermissions.Contains);
        }

        public static IReadOnlyCollection<string> ExpandRequestedPermissions(IEnumerable<string> permissions)
        {
            var expanded = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);

            foreach (var permission in permissions)
            {
                if (LegacyPermissionAliases.TryGetValue(permission, out var aliases))
                {
                    foreach (var alias in aliases)
                    {
                        expanded.Add(alias);
                    }
                }
            }

            return expanded;
        }

        public static IReadOnlyCollection<string> ExpandGrantedPermissions(IEnumerable<string> permissions)
        {
            var expanded = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);

            foreach (var permission in permissions)
            {
                foreach (var aliasGroup in LegacyPermissionAliases)
                {
                    if (aliasGroup.Value.Any(alias => string.Equals(alias, permission, StringComparison.OrdinalIgnoreCase)))
                    {
                        expanded.Add(aliasGroup.Key);
                    }
                }
            }

            return expanded;
        }

        public static IReadOnlyCollection<string> GetDefaultPermissionsForRoles(IEnumerable<string> roleNames)
        {
            if (!IsHrRole(roleNames))
            {
                return Array.Empty<string>();
            }

            return HrDefaultPermissions;
        }
    }
}
