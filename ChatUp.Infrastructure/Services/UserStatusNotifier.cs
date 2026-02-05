using ChatUp.Application.Common.Interfaces;
using ChatUp.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Services
{

    public class UserStatusNotifier : IUserStatusNotifier
    {
        private readonly IHubContext<UserStatusHub> _hubContext;

        public UserStatusNotifier(IHubContext<UserStatusHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyStatusChanged(int userId, bool isOnline)
        {
            await _hubContext.Clients.All.SendAsync("UserStatusChanged", userId, isOnline, DateTime.UtcNow);
        }
    }
}
