
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace API.Services
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FindAsync(id);
        }

        public Task<IEnumerable<MessageDTO>> GetMessageThread(int currentUserId, int receiverId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<PagedList<MessageDTO>> GetUserMessages(MessageParam messageParam)
        {
            IQueryable<Message> messageQuery = _context.Messages
                .OrderByDescending(m => m.DateSent)
                .AsQueryable();

            messageQuery = messageParam.Container switch
            {
                "Inbox" => messageQuery.Where(m => m.Receiver.UserName == messageParam.Username),
                "Outbox" => messageQuery.Where(m => m.Sender.UserName == messageParam.Username),
                _ => messageQuery.Where(m => m.Receiver.UserName == messageParam.Username && m.DateRead == null)
            };

            IQueryable<MessageDTO> messages = messageQuery.ProjectTo<MessageDTO>(_mapper.ConfigurationProvider);

            return await PagedList<MessageDTO>.CreateAsync(messages, messageParam.PageNumber, messageParam.PageSize);
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}