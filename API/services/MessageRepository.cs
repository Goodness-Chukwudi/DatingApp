
using System;
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
using Microsoft.EntityFrameworkCore;

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

        public async Task<IEnumerable<MessageDTO>> GetMessageThread(string currentUsername, string otherUsername)
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .ThenInclude(p => p.Photos)
                .Where(m => m.Receiver.UserName == currentUsername
                    && m.ReceiverDeleted == false
                    && m.Sender.UserName == otherUsername
                    || m.Receiver.UserName == otherUsername
                    && m.Sender.UserName == currentUsername
                    && m.SenderDeleted == false)
                .OrderBy(m => m.DateSent)
                .ToListAsync();

            var unReadMessages = messages.Where(m => m.DateRead == null && m.Receiver.UserName == currentUsername).ToList();
            if (unReadMessages.Count() > 0)
            {
                foreach (var message in unReadMessages)
                {
                    message.DateRead = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }

            return _mapper.Map<IEnumerable<MessageDTO>>(messages);
        }

        public async Task<PagedList<MessageDTO>> GetUserMessages(MessageParam messageParam)
        {
            IQueryable<Message> messageQuery = _context.Messages
                .OrderByDescending(m => m.DateSent)
                .AsQueryable();

            messageQuery = messageParam.Container switch
            {
                "Inbox" => messageQuery.Where(m => m.Receiver.UserName == messageParam.Username && m.ReceiverDeleted == false),
                "Outbox" => messageQuery.Where(m => m.Sender.UserName == messageParam.Username),
                _ => messageQuery.Where(m => m.Receiver.UserName == messageParam.Username && m.DateRead == null)
            };

            var unReadMessages = await messageQuery
                .Where(m => m.DateRead == null && m.Receiver.UserName == messageParam.Username)
                .ToListAsync();

            if (unReadMessages.Count() > 0)
            {
                foreach (var message in unReadMessages)
                {
                    message.DateRead = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }

            IQueryable<MessageDTO> messages = messageQuery.ProjectTo<MessageDTO>(_mapper.ConfigurationProvider);

            return await PagedList<MessageDTO>.CreateAsync(messages, messageParam.PageNumber, messageParam.PageSize);
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}