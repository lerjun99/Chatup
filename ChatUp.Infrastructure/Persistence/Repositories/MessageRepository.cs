using ChatUp.Domain.Entities;
using ChatUp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Persistence.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ChatDBContext _context;

        public MessageRepository(ChatDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ChatMessage>> GetConversationAsync(int user1Id,int user2Id, int skip,int take)
        {
            return await _context.ChatMessages.AsNoTracking()
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                            (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderByDescending(m => m.Id) // newest first
                .Skip(skip)
                .Take(take)
                .OrderBy(m => m.Timestamp) // restore chronological order
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
        {
            try
            {
                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();
                return message;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving users: " + ex.Message);
            }
        
        }
    }
}
