using System.Security.Claims;

namespace OmniBizAI.Services.Auth;

public interface ITenantContext
{
    int? CurrentTenantId { get; }
    string? CurrentUserId { get; }
}

public class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public int? CurrentTenantId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity!.IsAuthenticated)
                return null;

            var tenantClaim = user.FindFirst("TenantId");
            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out var tenantId))
            {
                return tenantId;
            }
            return null;
        }
    }

    public string? CurrentUserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
