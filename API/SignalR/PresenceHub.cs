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
            await _tracker.HandleConnection(username, Context.ConnectionId);
            await Clients.Others.SendAsync("UserOnline", username);

            var onlineUsers = await _tracker.GetOnlineUsers();
            await Clients.All.SendAsync("OnlineUsers", onlineUsers);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string username = Context.User.GetUsername();
            await Clients.Others.SendAsync("UserOffline", username);

            var onlineUsers = await _tracker.GetOnlineUsers();
            await Clients.All.SendAsync("OnlineUsers", onlineUsers);

            await base.OnDisconnectedAsync(exception);
        }
    }
}