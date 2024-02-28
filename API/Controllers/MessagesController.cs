
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
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public MessagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO)
        {
            int userId = User.GetUserId();
            AppUser sender = await _unitOfWork.UserRepository.GetUserByIdAsync(userId);
            AppUser receiver = await _unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDTO.ReceiverUsername);

            if (receiver == null) return NotFound("Receiver not found");

            Message message = new Message
            {
                SenderUsername = sender.UserName,
                Sender = sender,
                ReceiverUsername = receiver.UserName,
                Receiver = receiver,
                Content = createMessageDTO.Content
            };

            _unitOfWork.MessageRepository.AddMessage(message);

            if (await _unitOfWork.Save())
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
            PagedList<MessageDTO> messages = await _unitOfWork.MessageRepository.GetUserMessages(messageParam);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread(string username)
        {
            string currentUsername = User.GetUsername();
            IEnumerable<MessageDTO> messages = await _unitOfWork.MessageRepository.GetMessageThread(currentUsername, username);
            return Ok(messages);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var currentUsername = User.GetUsername();
            var message = await _unitOfWork.MessageRepository.GetMessage(id);
            if (message.SenderUsername != currentUsername && message.ReceiverUsername != currentUsername)
                return Unauthorized();
            if (message.SenderUsername == currentUsername) message.SenderDeleted = true;
            if (message.ReceiverUsername == currentUsername) message.ReceiverDeleted = true;
            if (message.SenderDeleted && message.ReceiverDeleted) _unitOfWork.MessageRepository.DeleteMessage(message);
            if (await _unitOfWork.Save()) return Ok();

            return BadRequest("Error deleting message");
        }
    }

}