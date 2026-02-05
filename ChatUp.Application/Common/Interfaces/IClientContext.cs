using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Interfaces
{
    public interface IClientContext
    {
        string? IpAddress { get; }
        string? UserAgent { get; }
    }
}
