using ChatUp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{
    public class SlaHub : Hub
    {
        public async Task SubscribeTicket(int ticketId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
        }
    }
}
