using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using API.Extensions;

namespace API.SignalR
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker _tracker;

        public PresenceHub(PresenceTracker tracker)
        {
            _tracker = tracker;
        }

        public override async Task OnConnectedAsync()
        {
            string username = Context.User.GetUsername();
            var isNewConnection = await _tracker.HandleConnection(username, Context.ConnectionId);
            if (isNewConnection) await Clients.Others.SendAsync("UserOnline", username);

            var onlineUsers = await _tracker.GetOnlineUsers();
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string username = Context.User.GetUsername();
            var hasGoneOffline = await _tracker.HandleDisconnection(Context.User.GetUsername(), Context.ConnectionId);
            if (hasGoneOffline) await Clients.Others.SendAsync("UserOffline", username);

            await base.OnDisconnectedAsync(exception);
        }
    }
}