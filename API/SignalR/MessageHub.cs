
using System;
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
        private readonly MessageRepository _messageRepository;
        private readonly IMapper _mapper;
        private readonly UnitOfWork _unitOfWork;
        public MessageHub(MessageRepository messageRepository, IMapper mapper, UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageRepository = messageRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var receiver = httpContext.Request.Query["receiver"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), receiver);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var messages = await _messageRepository.GetMessageThread(Context.User.GetUsername(), receiver);
            await Clients.Group(groupName).SendAsync("ReceivedMessages", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
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

            _unitOfWork.MessageRepository.AddMessage(message);

            if (await _unitOfWork.Save())
            {
                var groupName = GetGroupName(Context.User.GetUsername(), receiver.UserName);
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDTO>(message));
            }
        }

        private string GetGroupName(string senderName, string receiverName)
        {
            var startWithSender = string.CompareOrdinal(senderName, receiverName) < 0;

            return startWithSender ? $"{senderName}-{receiverName}" : $"{receiverName}-{senderName}";
        }
    }
}