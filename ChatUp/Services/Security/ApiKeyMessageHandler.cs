using ChatUp.Application.Auditing;
using ChatUp.Application.Common.Security;

namespace ChatUp.Services.Security
{
    /// <summary>
    /// Adds this UI's API key to every request sent to the ChatUp API. It is attached to all HttpClients (including
    /// the injected one), but only ever sends the key to the API host — never to third-party services. A request that
    /// already carries a key (e.g. testing a pasted key) is left untouched.
    ///
    /// For the audit log it also sends who is acting: the signed-in user's session token (the API verifies it) and
    /// the browser's IP address, device and session id.
    /// </summary>
    public sealed class ApiKeyMessageHandler : DelegatingHandler
    {
        private readonly UiApiKeyStore _store;
        private readonly CircuitServicesAccessor _circuit;

        public ApiKeyMessageHandler(UiApiKeyStore store, CircuitServicesAccessor circuit)
        {
            _store = store;
            _circuit = circuit;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (IsApiRequest(request.RequestUri))
            {
                if (!request.Headers.Contains(ApiKeyDefaults.HeaderName))
                    request.Headers.TryAddWithoutValidation(ApiKeyDefaults.HeaderName, _store.CurrentKey);

                if (_circuit.Services?.GetService(typeof(ActivityIdentity)) is ActivityIdentity identity)
                {
                    if (!string.IsNullOrEmpty(identity.UserToken) && !request.Headers.Contains(ApiKeyDefaults.UserTokenHeader))
                        request.Headers.TryAddWithoutValidation(ApiKeyDefaults.UserTokenHeader, identity.UserToken);
                    if (!string.IsNullOrEmpty(identity.ClientIp))
                        request.Headers.TryAddWithoutValidation(ActivityHeaders.ClientIp, identity.ClientIp);
                    if (!string.IsNullOrEmpty(identity.UserAgent))
                        request.Headers.TryAddWithoutValidation(ActivityHeaders.UserAgent, identity.UserAgent);
                    request.Headers.TryAddWithoutValidation(ActivityHeaders.SessionId, identity.SessionId);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }

        public static bool IsApiRequest(Uri? uri)
        {
            if (uri == null || !uri.IsAbsoluteUri)
                return false;

            if (!Uri.TryCreate(AppConfig.ChatUrl, UriKind.Absolute, out var api))
                return false;

            return Uri.Compare(uri, api, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
