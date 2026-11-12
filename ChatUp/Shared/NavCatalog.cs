using ChatUp.Services;

namespace ChatUp.Shared
{
    /// <summary>One navigation destination, shared by the sidebar and the mobile tab bar.</summary>
    public sealed record NavItem(
        string Key,
        string Label,
        string Icon,
        string Section,
        string Path,
        bool NeedsUserToken,
        string[] MatchSegments,
        Func<int?, bool> IsVisibleFor);

    /// <summary>
    /// Single source of truth for the app's navigation: labels, icons, who can see what,
    /// how URLs are built (some routes carry the encrypted user id) and which item is active.
    /// </summary>
    public static class NavCatalog
    {
        public const string WorkspaceSection = "Workspace";
        public const string AdminSection = "Administration";

        // UserType: 1 Admin, 2 Support, 3 Client, 4 Developer/QA, 5 HR
        public static readonly NavItem[] Items =
        {
            new("dashboard", "Dashboard", "fa-solid fa-chart-simple", WorkspaceSection, "/dashboard", true,
                new[] { "dashboard" }, t => t != 5),
            new("tickets", "Tickets", "fa-solid fa-ticket", WorkspaceSection, "/ticketlist", true,
                new[] { "ticketlist", "tickets" }, t => t != 5),
            new("inbox", "Inbox", "fa-solid fa-comments", WorkspaceSection, "/inbox", true,
                new[] { "inbox" }, t => t != 5),
            //new("calendar", "Calendar", "fa-regular fa-calendar", WorkspaceSection, "/calendar", true,
            //    new[] { "calendar" }, t => t != 5),
            //new("applicants", "Applicants", "fa-solid fa-user-tie", WorkspaceSection, "/applicants", false,
            //    new[] { "applicants" }, t => t is 1 or 5),
            new("contracts", "Contracts", "fa-solid fa-file-contract", AdminSection, "/contractlist", false,
                new[] { "contractlist", "contracts" }, t => t == 1),
            new("assignments", "Assignments", "fa-solid fa-diagram-project", AdminSection, "/assignments", true,
                new[] { "assignments" }, t => t == 1),
            new("users", "Users", "fa-solid fa-users-gear", AdminSection, "/users", false,
                new[] { "users", "user-registers" }, t => t == 1),
            new("apikeys", "API keys", "fa-solid fa-key", AdminSection, "/settings/api-keys", false,
                new[] { "settings" }, t => t == 1),
        };

        public static IEnumerable<NavItem> VisibleFor(int? userType) =>
            Items.Where(i => i.IsVisibleFor(userType));

        /// <summary>Builds the item's URL, or null when it needs a signed-in user that isn't known yet.</summary>
        public static string? BuildUrl(NavItem item, int? userId, LinkService linkService)
        {
            if (!item.NeedsUserToken)
                return item.Path;

            if (!userId.HasValue)
                return null;

            // 🔐 Encrypt user ID
            var token = linkService.Protect(userId.Value.ToString());
            return $"{item.Path}/{Uri.EscapeDataString(token)}";
        }

        /// <summary>The item whose route matches the first path segment of <paramref name="absoluteUri"/>.</summary>
        public static NavItem? Active(string absoluteUri)
        {
            var path = new Uri(absoluteUri).AbsolutePath.Trim('/');
            var first = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            return Items.FirstOrDefault(i => i.MatchSegments.Contains(first, StringComparer.OrdinalIgnoreCase));
        }

        public static string RoleLabel(int? userType) => userType switch
        {
            1 => "Administrator",
            2 => "Support",
            3 => "Client",
            4 => "Developer",
            5 => "HR",
            _ => "Member"
        };
    }
}
