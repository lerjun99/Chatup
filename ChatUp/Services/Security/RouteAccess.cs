namespace ChatUp.Services.Security
{
    public enum RouteDecision
    {
        Allow,
        /// <summary>Signed in, but this role may not open the route.</summary>
        Forbidden,
        /// <summary>The link carries another user's id: send the user to the same page with their own id.</summary>
        Redirect
    }

    /// <summary>
    /// Who may open which route. Every page that uses MainLayout goes through here after the session is confirmed,
    /// so typing or pasting a URL can't reach a page the role can't see in the menu.
    /// UserType: 1 Admin, 2 Support, 3 Client, 4 Developer/QA, 5 HR.
    /// </summary>
    public static class RouteAccess
    {
        /// <summary>
        /// Pages that must work without a session: sign-in, password recovery and the session-expired notice.
        /// (Logout navigates to these after the session is cleared, so they must never be blocked.)
        /// </summary>
        private static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "", "login", "session-expired", "change-password", "verify-email", "otp-verification"
        };

        /// <summary>Routes whose id segment is the signed-in user's own (encrypted) id.</summary>
        private static readonly HashSet<string> OwnUserRoutes = new(StringComparer.OrdinalIgnoreCase)
        {
            "dashboard", "ticketlist", "inbox", "assignments", "chat", "calendar"
        };

        public static bool IsPublic(string absoluteUri) => PublicRoutes.Contains(FirstSegment(Segments(absoluteUri)));

        public static RouteDecision Evaluate(string absoluteUri, int? userId, int? userType, LinkService links, out string? redirectTo)
        {
            redirectTo = null;
            var segments = Segments(absoluteUri);
            var first = FirstSegment(segments);

            if (PublicRoutes.Contains(first))
                return RouteDecision.Allow;

            if (!IsRoleAllowed(first, segments, userId, userType, links))
                return RouteDecision.Forbidden;

            // /dashboard/{someone else's id} → /dashboard/{own id}
            if (OwnUserRoutes.Contains(first) && userId.HasValue)
            {
                // /inbox/ticket/{userId}/{ticketNo}/{status}: the id is the third segment
                var index = first.Equals("inbox", StringComparison.OrdinalIgnoreCase)
                            && segments.Length > 1
                            && segments[1].Equals("ticket", StringComparison.OrdinalIgnoreCase) ? 2 : 1;

                if (segments.Length > index && DecryptInt(segments[index], links) != userId)
                {
                    segments[index] = links.Protect(userId.Value.ToString());
                    redirectTo = "/" + string.Join('/', segments) + new Uri(absoluteUri).Query;
                    return RouteDecision.Redirect;
                }
            }

            return RouteDecision.Allow;
        }

        private static bool IsRoleAllowed(string first, string[] segments, int? userId, int? userType, LinkService links)
        {
            switch (first.ToLowerInvariant())
            {
                // Workspace: everyone except HR
                case "dashboard":
                case "ticketlist":
                case "tickets":
                case "inbox":
                case "chat":
                case "calendar":
                    return userType is not null and not 5;

                // Administration
                case "contractlist":
                case "contracts":
                case "assignments":
                case "users":
                case "audit-log":
                case "settings":
                case "counter":     // template pages
                case "fetchdata":
                    return userType == 1;

                // Admins edit anyone; everyone else only their own profile ("My profile")
                case "user-registers":
                    return userType == 1
                           || (userId.HasValue && segments.Length > 1 && DecryptInt(segments[1], links) == userId);

                case "applicants":
                    return userType is 1 or 5;

                // Unknown routes fall through to the router's "not found"
                default:
                    return userType is not null;
            }
        }

        private static int? DecryptInt(string segment, LinkService links) =>
            int.TryParse(links.Unprotect(segment), out var value) ? value : null;

        private static string[] Segments(string absoluteUri) =>
            new Uri(absoluteUri).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        private static string FirstSegment(string[] segments) => segments.FirstOrDefault() ?? string.Empty;
    }
}
