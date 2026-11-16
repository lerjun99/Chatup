using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ChatUp.Services.Security
{
    /// <summary>
    /// Who is using this browser tab, for the API's audit log: the signed-in user's session token (verified by the
    /// API), the browser's own IP address and device (the API otherwise only sees this web server) and a session id
    /// that links Login → … → Logout.
    /// </summary>
    public sealed class ActivityIdentity
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }

        /// <summary>UserAccount.JWToken of the signed-in user; null before sign-in.</summary>
        public string? UserToken { get; set; }
    }

    /// <summary>
    /// HttpClient handlers run in their own DI scope, not the Blazor circuit's. This hands them the circuit's services
    /// for the duration of each circuit activity (the documented .NET 8 pattern).
    /// </summary>
    public sealed class CircuitServicesAccessor
    {
        private static readonly AsyncLocal<IServiceProvider?> Current = new();

        public IServiceProvider? Services
        {
            get => Current.Value;
            set => Current.Value = value;
        }
    }

    public sealed class ServicesAccessorCircuitHandler : CircuitHandler
    {
        private readonly IServiceProvider _services;
        private readonly CircuitServicesAccessor _accessor;

        public ServicesAccessorCircuitHandler(IServiceProvider services, CircuitServicesAccessor accessor)
        {
            _services = services;
            _accessor = accessor;
        }

        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next) =>
            async context =>
            {
                _accessor.Services = _services;
                await next(context);
                _accessor.Services = null;
            };
    }
}
