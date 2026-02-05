using ChatUp.Domain.Entities;

namespace ChatUp.Domain.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<ChatMessage>> GetConversationAsync(int user1Id, int user2Id, int skip, int take);
        Task<ChatMessage> AddMessageAsync(ChatMessage message);
    }
}
