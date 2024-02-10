
using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
        {
            _mapper = mapper;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO)
        {
            int userId = User.GetUserId();
            AppUser sender = await _userRepository.GetUserByIdAsync(userId);
            AppUser receiver = await _userRepository.GetUserByUsernameAsync(createMessageDTO.ReceiverUsername);

            if (receiver == null) return NotFound("Receiver not found");

            Message message = new Message
            {
                SenderUsername = sender.UserName,
                Sender = sender,
                ReceiverUsername = receiver.UserName,
                Receiver = receiver,
                Content = createMessageDTO.Content
            };

            _messageRepository.AddMessage(message);

            if (await _messageRepository.SaveAll())
            {
                MessageDTO newMessage = _mapper.Map<MessageDTO>(message);
                return newMessage;
            }

            return BadRequest("Action failed");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessages([FromQuery] MessageParam messageParam)
        {
            messageParam.Username = User.GetUsername();
            PagedList<MessageDTO> messages = await _messageRepository.GetUserMessages(messageParam);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread(string username)
        {
            string currentUsername = User.GetUsername();
            IEnumerable<MessageDTO> messages = await _messageRepository.GetMessageThread(currentUsername, username);
            return Ok(messages);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var currentUsername = User.GetUsername();

            var message = await _messageRepository.GetMessage(id);

            if (message.Sender.UserName != currentUsername && message.Receiver.UserName != currentUsername)
                return Unauthorized();

            if (message.Sender.UserName == currentUsername) message.SenderDeleted = true;
            if (message.Receiver.UserName == currentUsername) message.ReceiverDeleted = true;
            if (message.SenderDeleted && message.ReceiverDeleted) _messageRepository.DeleteMessage(message);

            if (await _messageRepository.SaveAll()) return Ok();

            return BadRequest("Error deleting message");
        }
    }

}