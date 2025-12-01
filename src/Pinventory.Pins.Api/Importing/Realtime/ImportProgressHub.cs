using Microsoft.AspNetCore.SignalR;

using Pinventory.ApiDefaults;

namespace Pinventory.Pins.Api.Importing.Realtime;

public sealed class ImportProgressHub : Hub<IImportProgressClient>
{
    public static string UserGroup(string userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetIdentifier();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }
}