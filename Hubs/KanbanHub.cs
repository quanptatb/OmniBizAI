using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OmniBizAI.Services;

namespace OmniBizAI.Hubs;

[Authorize]
public class KanbanHub(ITenantContext tenant) : Hub
{
    public static string TenantBoardGroup(Guid tenantId) => $"tenant:{tenantId}:kanban";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantBoardGroup(tenant.TenantId));
        await base.OnConnectedAsync();
    }
}
