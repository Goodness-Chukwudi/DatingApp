
using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IMessageRepository
    {
        void AddGroup(Group group);
        void RemoveConnection(Connection connection);
        Task<Group> GetGroup(string groupName);
        Task<Group> GetGroupForConnection(string connectionId);
        Task<Connection> GetConnection(string connectionId);
        void AddMessage(Message message);
        void DeleteMessage(Message message);
        Task<Message> GetMessage(int id);
        Task<PagedList<MessageDTO>> GetUserMessages(MessageParam messageParams);
        Task<IEnumerable<MessageDTO>> GetMessageThread(string currentUsername, string otherUsername);
        Task<bool> SaveAll();
    }
}