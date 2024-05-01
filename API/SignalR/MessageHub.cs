
using System;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Services;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMapper _mapper;
        private readonly UnitOfWork _unitOfWork;
        private readonly PresenceTracker _presenceTracker;
        private readonly IHubContext<PresenceHub> _presenceHub;
        public MessageHub(IMapper mapper, UnitOfWork unitOfWork, PresenceTracker presenceTracker, IHubContext<PresenceHub> presenceHub)
        {
            _presenceHub = presenceHub;
            _presenceTracker = presenceTracker;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var receiver = httpContext.Request.Query["receiver"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), receiver);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var group = await AddConnectionToGroup(groupName);
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);

            var messages = await _unitOfWork.MessageRepository.GetMessageThread(Context.User.GetUsername(), receiver);
            await Clients.Caller.SendAsync("ReceivedMessages", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveConnectionFromGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDTO newMessage)
        {
            int userId = Context.User.GetUserId();
            var sender = await _unitOfWork.UserRepository.GetUserByIdAsync(userId);
            var receiver = await _unitOfWork.UserRepository.GetUserByUsernameAsync(newMessage.ReceiverUsername);

            if (receiver == null) throw new HubException("Receiver not found");

            var message = new Message
            {
                SenderUsername = sender.UserName,
                Sender = sender,
                ReceiverUsername = receiver.UserName,
                Receiver = receiver,
                Content = newMessage.Content
            };

            var groupName = GetGroupName(Context.User.GetUsername(), receiver.UserName);
            var group = await _unitOfWork.MessageRepository.GetGroup(groupName);
            if (group.Connections.Any(x => x.Username == receiver.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await _presenceTracker.GetUserConnections(receiver.UserName);
                if (connections != null)
                {
                    await _presenceHub.Clients
                        .Clients(connections)
                        .SendAsync(
                            "NewMessageNotification",
                            new
                            {
                                username = sender.UserName,
                                knownAs = sender.NickName
                            });
                }
            }

            _unitOfWork.MessageRepository.AddMessage(message);

            if (await _unitOfWork.Save())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDTO>(message));
            }
        }

        private async Task<Group> AddConnectionToGroup(string groupName)
        {
            var group = await _unitOfWork.MessageRepository.GetGroup(groupName);
            if (group == null)
            {
                group = new Group(groupName);
                _unitOfWork.MessageRepository.AddGroup(group);
            }

            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());
            group.Connections.Add(connection);

            if (await _unitOfWork.Save()) return group;
            throw new HubException($"Failed to add connection to group {group.Name}");
        }

        private async Task<Group> RemoveConnectionFromGroup()
        {
            var group = await _unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _unitOfWork.MessageRepository.RemoveConnection(connection);

            if (await _unitOfWork.Save()) return group;
            throw new HubException($"Unable to remove connection from group {group.Name}");
        }

        private string GetGroupName(string senderName, string receiverName)
        {
            var startWithSender = string.CompareOrdinal(senderName, receiverName) < 0;

            return startWithSender ? $"{senderName}-{receiverName}" : $"{receiverName}-{senderName}";
        }
    }
}