using ChatUp.Application.Common.Security;

namespace ChatUp.Services.Security
{
    /// <summary>
    /// Adds this UI's API key to every request sent to the ChatUp API. It is attached to all
    /// HttpClients (including the injected one), but only ever sends the key to the API host —
    /// never to third-party services. A request that already carries a key (e.g. testing a
    /// pasted key) is left untouched.
    /// </summary>
    public sealed class ApiKeyMessageHandler : DelegatingHandler
    {
        private readonly UiApiKeyStore _store;

        public ApiKeyMessageHandler(UiApiKeyStore store)
        {
            _store = store;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (IsApiRequest(request.RequestUri) && !request.Headers.Contains(ApiKeyDefaults.HeaderName))
                request.Headers.TryAddWithoutValidation(ApiKeyDefaults.HeaderName, _store.CurrentKey);

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
