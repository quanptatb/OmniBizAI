namespace OmniBizAI.Models.Auth;

public class PermissionsConfig
{
    public List<RoutePermissionRule> RoutePermissionRules { get; set; } = new();
    public List<RolePermissionConfig> RolePermissions { get; set; } = new();
}

public class RoutePermissionRule
{
    public string Area { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequiredPermissionCode { get; set; } = string.Empty;
}

public class RolePermissionConfig
{
    public string RoleCode { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
