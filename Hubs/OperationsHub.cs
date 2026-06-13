using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OmniBizAI.Services;

namespace OmniBizAI.Hubs;

[Authorize]
public class OperationsHub(ITenantContext tenant) : Hub
{
    public static string TenantGroup(Guid tenantId) => $"tenant:{tenantId}:operations";
    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenant.TenantId));
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(tenant.UserId));
        await base.OnConnectedAsync();
    }
}
