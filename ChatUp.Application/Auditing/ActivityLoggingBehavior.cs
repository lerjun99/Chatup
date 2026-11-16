using System.Diagnostics;
using ChatUp.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatUp.Application.Auditing
{
    /// <summary>
    /// Writes one audit row for every command that runs through MediatR: who ran it, the outcome, how long it took,
    /// the handler's note and every data change the EF interceptor captured while it ran. Queries aren't logged
    /// here (page visits come from the web app).
    /// </summary>
    public sealed class ActivityLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IActivityScope _scope;
        private readonly IActivityLogQueue _queue;
        private readonly ILogger<ActivityLoggingBehavior<TRequest, TResponse>> _logger;

        public ActivityLoggingBehavior(IActivityScope scope, IActivityLogQueue queue, ILogger<ActivityLoggingBehavior<TRequest, TResponse>> logger)
        {
            _scope = scope;
            _queue = queue;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            ActivityScope.Current ??= _scope;

            var descriptor = ActivityDescriptor.For(request.GetType());

            if (!descriptor.IsCommand && !descriptor.Skip)
                return await next();

            var changesFrom = _scope.ChangeCount;
            var notesFrom = _scope.NoteCount;

            if (descriptor.Skip)
            {
                try
                {
                    return await next();
                }
                finally
                {
                    _scope.Take(changesFrom, notesFrom); // not audited: drop what it changed
                }
            }

            var outermost = _scope.CommandDepth == 0;
            _scope.CommandDepth++;

            var stopwatch = Stopwatch.StartNew();
            TResponse? response = default;
            Exception? failure = null;

            try
            {
                response = await next();
                return response;
            }
            catch (Exception ex)
            {
                failure = ex;
                throw;
            }
            finally
            {
                _scope.CommandDepth--;

                // Nested commands (a handler sending another command) are folded into the outer row
                if (outermost)
                {
                    try
                    {
                        var (changes, notes) = _scope.Take(changesFrom, notesFrom);
                        var row = Build(descriptor, request, response, failure, changes, notes, (int)stopwatch.ElapsedMilliseconds);
                        if (row != null)
                            _queue.Enqueue(row);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Audit row could not be built for {Request}", descriptor.RequestName);
                    }
                }
            }
        }

        private AdminActivityLog? Build(ActivityDescriptor descriptor, TRequest request, TResponse? response, Exception? failure,
            List<EntityChange> changes, List<AuditNote> notes, int durationMs)
        {
            var (isSuccess, message) = failure != null
                ? (false, failure.GetBaseException().Message)
                : ActivityDescriptor.ReadResult(response);

            var operation = ChangeFormatter.Humanize(descriptor.Operation);
            AdminActivityLog row;

            if (descriptor.IsAuth)
            {
                var (action, summary) = DescribeAuth(descriptor, isSuccess, message, operation);
                var category = descriptor.RequestName == "UnlockActivityDetailsCommand" ? ActivityCategories.Security : ActivityCategories.Auth;

                row = ActivityLogFactory.Create(_scope, category, action, descriptor.Module, summary);
                if (string.IsNullOrEmpty(row.UserName))
                    row.UserName = ChangeFormatter.Truncate(ActivityDescriptor.ReadActor(request), 256);
                if (row.UserId == null && isSuccess && descriptor.RequestName == "LoginCommand")
                    row.UserId = ActivityDescriptor.ReadLoginUserId(response);

                // Changes (login flags, password changed…) stay as detail; the summary is the fixed auth wording
                ActivityLogFactory.ApplyChanges(row, changes, notes);
                row.Summary = ChangeFormatter.Truncate(summary, 500);
            }
            else
            {
                var action = ActionFor(descriptor, changes);
                var past = ActivityLogFactory.PastTense(action);

                row = ActivityLogFactory.Create(_scope, ActivityCategories.Data, action, descriptor.Module, string.Empty);
                if (string.IsNullOrEmpty(row.UserName))
                    row.UserName = ChangeFormatter.Truncate(ActivityDescriptor.ReadActor(request), 256);

                ActivityLogFactory.ApplyChanges(row, changes, notes);

                if (notes.Count == 0 || string.IsNullOrWhiteSpace(row.Summary))
                {
                    var target = row.EntityId != null ? $" #{row.EntityId}" : string.Empty;
                    var subject = row.EntityName ?? descriptor.Module;
                    row.Summary = action is ActivityActions.Create or ActivityActions.Update or ActivityActions.Delete or ActivityActions.Restore
                        ? $"{past} {subject}{target}"
                        : $"{operation}{target}";
                }

                if (!isSuccess)
                    row.Summary = ChangeFormatter.Truncate($"Failed: {operation}", 500);
            }

            row.IsSuccess = isSuccess;
            row.DurationMs = durationMs;

            if (!isSuccess && !string.IsNullOrWhiteSpace(message))
                row.ErrorMessage = ChangeFormatter.Truncate(message, 1000);

            return row;
        }

        private static string ActionFor(ActivityDescriptor descriptor, IReadOnlyList<EntityChange> changes) => descriptor.Verb switch
        {
            "Save" or "Add" or "Create" or "Upsert" or "Register" or "Assign" or "Upload" or "Submit" =>
                changes.Count == 0 ? ActivityActions.Create : ActivityLogFactory.ActionFor(changes),
            "Delete" or "Remove" => ActivityActions.Delete,
            "Restore" => ActivityActions.Restore,
            "Update" or "Approve" or "Close" => changes.Count > 0 && changes[0].Action is ActivityActions.Delete or ActivityActions.Restore
                ? changes[0].Action
                : ActivityActions.Update,
            _ => descriptor.Operation.Length <= 50 ? descriptor.Operation : descriptor.Verb
        };

        private static (string Action, string Summary) DescribeAuth(ActivityDescriptor descriptor, bool isSuccess, string? message, string operation)
        {
            var reason = string.IsNullOrWhiteSpace(message) ? string.Empty : $": {message}";

            return descriptor.RequestName switch
            {
                "LoginCommand" => isSuccess
                    ? (ActivityActions.Login, "Signed in")
                    : (ActivityActions.LoginFailed, $"Sign-in failed{reason}"),

                "LogoutCommand" => (ActivityActions.Logout, "Signed out"),

                "UnlockActivityDetailsCommand" => isSuccess
                    ? (ActivityActions.ProtectedDetailsUnlocked, "Unlocked protected audit details")
                    : (ActivityActions.ProtectedDetailsUnlockFailed, $"Failed to unlock protected audit details{reason}"),

                _ => isSuccess
                    ? (descriptor.Operation, operation)
                    : (descriptor.Operation, $"{operation} failed{reason}")
            };
        }
    }
}
