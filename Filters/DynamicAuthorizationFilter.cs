using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using OmniBizAI.Services.Auth;

namespace OmniBizAI.Filters;

public class DynamicAuthorizationFilter(IPermissionService permissionService) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip if there's an AllowAnonymous attribute
        if (context.ActionDescriptor.EndpointMetadata.Any(em => em is Microsoft.AspNetCore.Authorization.IAllowAnonymous))
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user == null || !user.Identity!.IsAuthenticated)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var routeData = context.RouteData.Values;
        var area = routeData.TryGetValue("area", out var a) ? a?.ToString() ?? "" : "";
        var controller = routeData.TryGetValue("controller", out var c) ? c?.ToString() ?? "" : "";
        var action = routeData.TryGetValue("action", out var act) ? act?.ToString() ?? "" : "";
        var httpMethod = context.HttpContext.Request.Method;

        var requiredPermission = await permissionService.GetRequiredPermissionAsync(area, controller, action, httpMethod);

        // If no explicit rule, we can either default to deny or allow. The blueprint says "Authorization theo route đọc từ DB/cache"
        // Let's assume empty rule means allowed if authenticated (or you can require explicit rule).
        // For MVP, if requiredPermission is null or empty, we allow.
        if (string.IsNullOrEmpty(requiredPermission))
        {
            return;
        }

        var userRoles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        
        bool hasAccess = false;
        foreach (var role in userRoles)
        {
            if (await permissionService.HasPermissionAsync(role, requiredPermission))
            {
                hasAccess = true;
                break;
            }
        }

        if (!hasAccess)
        {
            context.Result = new ForbidResult();
        }
    }
}
