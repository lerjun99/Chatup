namespace ChatUp.Services.Security
{
    /// <summary>
    /// Adds the browser security headers (HSTS, CSP, X-Frame-Options, X-Content-Type-Options,
    /// Referrer-Policy, Permissions-Policy, COOP/CORP) to every response, pages and static files alike.
    ///
    /// appconfig.json → "SecurityHeaders":
    ///   "FrameAncestors": [ "https://hris.example.com" ]  sites allowed to show the portal in an iframe
    ///   "CspReportOnly": true                              browsers report CSP violations instead of blocking
    /// </summary>
    public sealed class SecurityHeadersMiddleware
    {
        private const string MonitorOrigin = "https://monitoring.odecci.tech";
        private const string CdnOrigin = "https://cdn.jsdelivr.net";

        private readonly RequestDelegate _next;
        private readonly string _cspTemplate;
        private readonly bool _reportOnly;

        public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _reportOnly = AppConfig.CspReportOnly;

            var frameAncestors = string.Join(" ", new[] { "'self'" }.Concat(AppConfig.FrameAncestors));
            var api = OriginOf(AppConfig.ChatUrl);
            // Development only: Visual Studio's Browser Link and hot reload connect to random localhost ports
            if (env.IsDevelopment())
                api += " http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*";

            // {ws} is filled per request: the Blazor circuit and hubs use a WebSocket to this host
            _cspTemplate = string.Join("; ", new[]
            {
                "default-src 'self'",
                "base-uri 'self'",
                "object-src 'none'",
                "form-action 'self'",
                $"frame-ancestors {frameAncestors}",
                $"script-src 'self' {CdnOrigin} {MonitorOrigin}",
                // Blazor, Bootstrap, FullCalendar and Fluent UI set inline styles
                "style-src 'self' 'unsafe-inline'",
                $"img-src 'self' data: blob: https: {api}".TrimEnd(),
                "font-src 'self' data:",
                $"connect-src 'self' {{ws}} {MonitorOrigin} {api}".TrimEnd(),
                "media-src 'self' blob:",
                "frame-src 'self' blob: data:",
                "worker-src 'self' blob:",
                "manifest-src 'self'"
            });
        }

        public Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;
            var request = context.Request;

            if (request.IsHttps)
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            var ws = (request.IsHttps ? "wss://" : "ws://") + request.Host.Value;
            var csp = _cspTemplate.Replace("{ws}", ws);
            if (request.IsHttps)
                csp += "; upgrade-insecure-requests";
            headers[_reportOnly ? "Content-Security-Policy-Report-Only" : "Content-Security-Policy"] = csp;

            // Older browsers; newer ones follow CSP frame-ancestors, which can list the allowed sites
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // Camera: face capture on the profile page. Clipboard: pasting images into chat.
            // Unload: blazor.server.js uses it to close the circuit when a tab closes; Chrome now
            // turns it off by default unless the site opts in.
            headers["Permissions-Policy"] =
                "unload=(self), " +
                "camera=(self), microphone=(), geolocation=(), payment=(), usb=(), serial=(), bluetooth=(), " +
                "magnetometer=(), gyroscope=(), accelerometer=(), clipboard-read=(self), clipboard-write=(self), " +
                "fullscreen=(self), browsing-topics=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-site";
            // Not set: Cross-Origin-Embedder-Policy: require-corp would block the CDN and monitor scripts

            return _next(context);
        }

        private static string OriginOf(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : string.Empty;
    }
}
