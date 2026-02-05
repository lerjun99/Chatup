using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IUserStatusNotifier
    {
        Task NotifyStatusChanged(int userId, bool isOnline);
    }
}
