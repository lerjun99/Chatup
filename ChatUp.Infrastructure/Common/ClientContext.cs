using ChatUp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Common
{
    public class ClientContext : IClientContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                    return null;

                var ip = context.Connection.RemoteIpAddress;

                if (ip == null)
                    return null;

                // Convert IPv6-mapped IPv4 → IPv4
                if (ip.IsIPv4MappedToIPv6)
                    ip = ip.MapToIPv4();

                // Convert ::1 → 127.0.0.1
                if (IPAddress.IsLoopback(ip))
                    return "127.0.0.1";

                return ip.ToString(); // ✅ 124.104.161.170
            }
        }

        public string? UserAgent =>
            _httpContextAccessor.HttpContext?
                .Request.Headers["User-Agent"]
                .ToString();
    }
}
