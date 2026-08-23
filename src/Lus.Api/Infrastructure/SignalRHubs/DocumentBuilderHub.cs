using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Lus.Authorization.Authentication;

namespace Lus.Infrastructure.SignalRHubs
{
    [Authorize(AuthenticationSchemes = CookieAuthSchemes.Api)]
    public class DocumentBuilderHub : Hub
    {
        public const string Path = "/hub/document-builder";

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnConnectedAsync();
        }

        public Task JoinSession(string sessionId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }
}
