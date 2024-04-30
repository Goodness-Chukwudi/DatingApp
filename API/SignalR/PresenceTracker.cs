using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.SignalR
{
    public class PresenceTracker
    {
        private static readonly Dictionary<string, List<string>> OnlineUsers = new Dictionary<string, List<string>>();

        public Task<bool> HandleConnection(string username, string connectionId)
        {
            bool isNewConnection = false;

            lock (OnlineUsers)
            {
                if (OnlineUsers.ContainsKey(username))
                {
                    OnlineUsers[username].Add(connectionId);
                }
                else
                {
                    OnlineUsers.Add(username, new List<string> { connectionId });
                    isNewConnection = true;
                }
            }

            return Task.FromResult(isNewConnection);
        }

        public Task<bool> HandleDisconnection(string username, string connectionId)
        {
            bool hasGoneOffline = false;
            lock (OnlineUsers)
            {
                if (OnlineUsers.ContainsKey(username))
                {
                    OnlineUsers[username].Remove(connectionId);
                    if (OnlineUsers[username].Count() == 0)
                    {
                        OnlineUsers.Remove(username);
                        hasGoneOffline = true;
                    }
                }
            }
            return Task.FromResult(hasGoneOffline);
        }

        public Task<string[]> GetOnlineUsers()
        {
            string[] usernames;

            lock (OnlineUsers)
            {
                usernames = OnlineUsers.OrderBy(x => x.Key).Select(x => x.Key).ToArray();
            }

            return Task.FromResult(usernames);
        }

        public Task<List<string>> GetUserConnections(string username)
        {
            List<string> connectionsIds;
            lock (OnlineUsers)
            {
                connectionsIds = OnlineUsers.GetValueOrDefault(username);
            }
            return Task.FromResult(connectionsIds);
        }

    }
}