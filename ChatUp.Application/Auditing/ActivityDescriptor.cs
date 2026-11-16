using System.Collections.Concurrent;
using System.Reflection;

namespace ChatUp.Application.Auditing
{
    /// <summary>
    /// What the audit log needs to know about a MediatR request type, worked out once per type: command or query,
    /// module (from the Features.&lt;Module&gt; namespace) and the operation name.
    /// </summary>
    public sealed class ActivityDescriptor
    {
        // High-volume background calls that aren't user actions worth auditing, and private content (chat messages,
        // which are already kept in the ticket conversation). Their data changes are dropped too.
        private static readonly HashSet<string> NotAudited = new(StringComparer.Ordinal)
        {
            "AddTicketMessageCommand",
            "SendMessageCommand",
            "MarkNotificationReadCommand",
            "MarkAllNotificationsReadCommand",
            "RecordLoginCommand",
            "RecordLogoutCommand",
            "SendScheduledEmailsCommand",
            "SyncHolidaysCommand",
        };

        // Requests that are queries by name even though they live in a Commands namespace
        private static readonly HashSet<string> QueriesByName = new(StringComparer.Ordinal)
        {
            "GetUserAssignedClientsCommand",
        };

        private static readonly HashSet<string> AuthRequests = new(StringComparer.Ordinal)
        {
            "LoginCommand",
            "LogoutCommand",
            "ChangePasswordCommand",
            "ChangeLogPasswordCommand",
            "SendOtpCommand",
            "ResendOtpCommand",
            "VerifyOtpCommand",
            "UnlockActivityDetailsCommand",
        };

        private static readonly string[] Suffixes = { "Command", "Query", "Queries", "Request", "Dto" };

        private static readonly ConcurrentDictionary<Type, ActivityDescriptor> Cache = new();

        public string RequestName { get; private init; } = string.Empty;
        public string Operation { get; private init; } = string.Empty;
        public string Verb { get; private init; } = string.Empty;
        public string Module { get; private init; } = string.Empty;
        public bool IsCommand { get; private init; }
        public bool IsAuth { get; private init; }
        public bool Skip { get; private init; }

        public static ActivityDescriptor For(Type requestType) => Cache.GetOrAdd(requestType, Build);

        private static ActivityDescriptor Build(Type requestType)
        {
            var name = requestType.Name;
            var ns = requestType.Namespace ?? string.Empty;

            var operation = name;
            foreach (var suffix in Suffixes)
            {
                if (operation.Length > suffix.Length && operation.EndsWith(suffix, StringComparison.Ordinal))
                {
                    operation = operation[..^suffix.Length];
                    break;
                }
            }

            var verbEnd = 1;
            while (verbEnd < operation.Length && !char.IsUpper(operation[verbEnd]))
                verbEnd++;
            var verb = operation[..Math.Min(verbEnd, operation.Length)];

            var rawModule = ModuleFromNamespace(ns, "Features") ?? ModuleFromNamespace(ns, "Application") ?? "General";

            // ChatUp keeps commands in *.Commands namespaces; a few live elsewhere but say so in their name
            var isCommand = !QueriesByName.Contains(name) && verb != "Get" &&
                            (ns.Contains(".Commands", StringComparison.Ordinal) || name.EndsWith("Command", StringComparison.Ordinal));

            return new ActivityDescriptor
            {
                RequestName = name,
                Operation = operation,
                Verb = verb,
                Module = ModuleLabel(rawModule),
                IsCommand = isCommand,
                IsAuth = AuthRequests.Contains(name) || rawModule is "Auth" or "EmailOTP",
                Skip = NotAudited.Contains(name)
            };
        }

        private static string ModuleLabel(string raw) => raw switch
        {
            "TicketMessage" => "Ticket chat",
            "UserRegistration" => "Users",
            "User" => "Assignments",
            "UserApplicant" => "Applicants",
            "EmailOTP" => "Password reset",
            "AutoReply" => "Auto reply",
            "Projects" => "Projects",
            "Ticket" => "Tickets",
            _ => ChangeFormatter.Humanize(raw)
        };

        private static string? ModuleFromNamespace(string ns, string marker)
        {
            var parts = ns.Split('.');
            var index = Array.IndexOf(parts, marker);
            return index >= 0 && index + 1 < parts.Length ? parts[index + 1] : null;
        }

        /// <summary>Reads success / message from a handler response (bool, IsSuccess/Success + Message properties).</summary>
        public static (bool IsSuccess, string? Message) ReadResult(object? response)
        {
            switch (response)
            {
                case null:
                    return (true, null);
                case bool b:
                    return (b, b ? null : "The operation did not complete.");
            }

            var type = response.GetType();

            // Auth responses wrap the outcome: { BaseResult: { MsgCode: "Ok" | "Error", Msg } }
            if (type.GetProperty("BaseResult")?.GetValue(response) is { } inner &&
                inner.GetType().GetProperty("MsgCode")?.GetValue(inner) is string msgCode)
            {
                var msg = inner.GetType().GetProperty("Msg")?.GetValue(inner) as string;
                var ok = string.Equals(msgCode, "Ok", StringComparison.OrdinalIgnoreCase);
                return (ok, ok ? null : msg);
            }

            var success = type.GetProperty("IsSuccess", BindingFlags.Public | BindingFlags.Instance)
                          ?? type.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance);
            var message = type.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);

            var isSuccess = success?.PropertyType == typeof(bool) ? (bool)success.GetValue(response)! : true;
            return (isSuccess, message?.GetValue(response) as string);
        }

        /// <summary>The signed-in user's id from a successful login response ({ Data: { Id } }).</summary>
        public static int? ReadLoginUserId(object? response)
        {
            var data = response?.GetType().GetProperty("Data")?.GetValue(response);
            return data?.GetType().GetProperty("Id")?.GetValue(data) is int id && id > 0 ? id : null;
        }

        /// <summary>For anonymous calls (sign-in, OTP, password reset) the user is named in the request itself.</summary>
        public static string? ReadActor(object request)
        {
            var type = request.GetType();
            foreach (var name in new[] { "Username", "UserName", "Email", "EmailAddress" })
            {
                if (type.GetProperty(name)?.GetValue(request) is string value && !string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }
    }
}
