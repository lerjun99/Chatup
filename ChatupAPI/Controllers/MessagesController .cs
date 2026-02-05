
using ChatUp.Hubs;
using DBContext;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Collections.Concurrent;
using Tools;

namespace ChatupAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ChatDBContext _context;

        public MessagesController(ChatDBContext context)
        {
            _context = context;
        }

        [HttpGet("conversation/{user1Id}/{user2Id}")]
        public async Task<ActionResult<IEnumerable<ChatMessage>>> GetConversation(int user1Id, int user2Id)
        {
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                            (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost]
        public async Task<ActionResult<ChatMessage>> SendMessage(ChatMessageDto messageDto)
        {
            var message = new ChatMessage
            {
                SenderId = messageDto.SenderId,
                ReceiverId = messageDto.ReceiverId,
                Text = messageDto.Text,
                Timestamp = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var savedMsg = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstAsync(m => m.Id == message.Id);

            // Notify via SignalR
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<ChatHub>>();
            await hubContext.Clients.User(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", savedMsg);
            await hubContext.Clients.User(message.SenderId.ToString()).SendAsync("ReceiveMessage", savedMsg);

            return Ok(savedMsg);
        }
    }

   
}

