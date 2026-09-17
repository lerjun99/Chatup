using ChatUp.Application.Common.Interfaces;
using ChatUp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatUp.Infrastructure.Services
{
    public class SupportResponseReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SupportResponseReminderService> _logger;
        private readonly SupportNotificationSettings _settings;

        public SupportResponseReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<SupportResponseReminderService> logger,
            SupportNotificationSettings settings)
        {
            _scopeFactory = scopeFactory
                ?? throw new ArgumentNullException(nameof(scopeFactory));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));

            _settings = settings
                ?? throw new InvalidOperationException(
                    "SupportNotificationSettings could not be loaded.");

            if (_settings.ReminderMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "SupportNotification:ReminderMinutes must be greater than 0.");
            }

            _logger.LogInformation(
                "Support Response Reminder configuration loaded. " +
                "Default Email: {Email}, ReminderMinutes: {ReminderMinutes}",
                _settings.Email,
                _settings.ReminderMinutes);
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Support Response Reminder Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckUnansweredMessages(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error checking unanswered support messages.");
                }

                try
                {
                    // Check every minute
                    await Task.Delay(
                        TimeSpan.FromMinutes(5),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Support Response Reminder Service stopped.");
        }

        // ============================================================
        // CHECK UNANSWERED TICKETS
        // ============================================================

        private async Task CheckUnansweredMessages(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<IChatDBContext>();

            var emailService =
                scope.ServiceProvider
                    .GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;

            var cutoff =
                now.AddMinutes(
                    -_settings.ReminderMinutes);

            _logger.LogInformation(
                "Checking unanswered support messages. " +
                "UTC Now: {Now}, Cutoff: {Cutoff}, ReminderMinutes: {Minutes}",
                now,
                cutoff,
                _settings.ReminderMinutes);

            // ========================================================
            // GET OPEN TICKETS ONLY
            // ========================================================

            var tickets =
                await context.Tickets
                    .Where(t =>
                        // Do not process CLOSED tickets
                        t.Status != TicketStatus.Closed &&

                        // Must have a client message
                        t.LastClientMessageAt.HasValue &&

                        // Client message must be old enough
                        t.LastClientMessageAt.Value <= cutoff &&

                        // Reminder has not already been sent
                        !t.SupportResponseReminderSent &&

                        // Support has not replied after client message
                        (
                            !t.LastSupportReplyAt.HasValue ||
                            t.LastSupportReplyAt.Value <
                            t.LastClientMessageAt.Value
                        ))
                    .ToListAsync(
                        cancellationToken);

            _logger.LogInformation(
                "Found {Count} ticket(s) awaiting support response.",
                tickets.Count);

            // ========================================================
            // PROCESS EACH TICKET
            // ========================================================

            foreach (var ticket in tickets)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await SendSupportReminder(
                        context,
                        emailService,
                        ticket,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed processing support reminder. " +
                        "TicketId: {TicketId}, TicketNo: {TicketNo}",
                        ticket.Id,
                        ticket.TicketNo);
                }
            }

            // ========================================================
            // SAVE
            // ========================================================

            await context.SaveChangesAsync(
                cancellationToken);
        }

        // ============================================================
        // SEND SUPPORT REMINDER
        // ============================================================

        private async Task SendSupportReminder(
            IChatDBContext context,
            IEmailService emailService,
            Ticket ticket,
            CancellationToken cancellationToken)
        {
            // ========================================================
            // SAFETY CHECK - CLOSED TICKET
            // ========================================================

            if (ticket.Status == TicketStatus.Closed)
            {
                _logger.LogInformation(
                    "Ticket {TicketNo} is closed. " +
                    "Skipping support reminder.",
                    ticket.TicketNo);

                return;
            }

            // ========================================================
            // CLIENT ID
            // ========================================================

            if (!ticket.ClientId.HasValue)
            {
                _logger.LogWarning(
                    "Ticket {TicketNo} does not have a ClientId. " +
                    "Cannot determine assigned support.",
                    ticket.TicketNo);

                return;
            }

            var clientId =
                ticket.ClientId.Value;
            var clientIdDisplay = $"ODC-SP-{clientId:D3}";

            // ========================================================
            // GET LATEST CLIENT MESSAGE
            // ========================================================

            var lastClientMessage =
                await context.TicketMessages
                    .Where(m =>
                        m.TicketId == ticket.Id &&
                        m.IsUser)
                    .OrderByDescending(m => m.DateCreated)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (lastClientMessage == null)
            {
                _logger.LogWarning(
                    "No client message found for Ticket {TicketNo}.",
                    ticket.TicketNo);

                return;
            }

            // ========================================================
            // NULL DATE CHECK
            // ========================================================

            if (!lastClientMessage.DateCreated.HasValue)
            {
                _logger.LogWarning(
                    "Latest client message has no DateCreated. " +
                    "TicketNo: {TicketNo}",
                    ticket.TicketNo);

                return;
            }

            var clientMessageDate =
                lastClientMessage.DateCreated.Value;

            // ========================================================
            // CHECK REMINDER TIME
            // ========================================================

            var cutoff =
                DateTime.UtcNow.AddMinutes(
                    -_settings.ReminderMinutes);

            if (clientMessageDate > cutoff)
            {
                _logger.LogInformation(
                    "Latest client message for Ticket {TicketNo} " +
                    "is not old enough for reminder.",
                    ticket.TicketNo);

                return;
            }

            // ========================================================
            // CHECK SUPPORT REPLY
            // ========================================================

            var latestSupportMessage =
                await context.TicketMessages
                    .Where(m =>
                        m.TicketId == ticket.Id &&
                        !m.IsUser &&
                        m.DateCreated.HasValue &&
                        m.DateCreated.Value > clientMessageDate)
                    .OrderByDescending(m => m.DateCreated)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (latestSupportMessage != null)
            {
                _logger.LogInformation(
                    "Support already replied to Ticket {TicketNo}. " +
                    "Skipping reminder.",
                    ticket.TicketNo);

                return;
            }

            // ========================================================
            // GET SUPPORT ASSIGNED TO CLIENT
            // ========================================================

            var supportUsers =
                await context.UserClientAssignments
                    .Where(a =>
                        a.ClientId == clientId &&
                        a.UserId.HasValue &&
                        a.User != null &&
                        a.UserType != null &&
                        a.UserType != 1 && // Exclude clients
                        a.User.IsDeleted == 0 &&
                        !string.IsNullOrWhiteSpace(
                            a.User.EmailAddress))
                    .Select(a => new
                    {
                        UserId = a.UserId,
                        Username = a.User.Username,
                        FullName = a.User.FullName,
                        EmailAddress = a.User.EmailAddress
                    })
                    .AsNoTracking()
                    .ToListAsync(
                        cancellationToken);

            // ========================================================
            // REMOVE DUPLICATE EMAILS
            // ========================================================

            var supportEmails =
                supportUsers
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.EmailAddress))
                    .Select(x =>
                        x.EmailAddress!.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            // ========================================================
            // NO ASSIGNED SUPPORT
            // ========================================================

            if (!supportEmails.Any())
            {
                _logger.LogWarning(
                    "No assigned support user with a valid email " +
                    "was found for ClientId {ClientId}. " +
                    "TicketNo: {TicketNo}.",
                    clientId,
                    ticket.TicketNo);

                return;
            }

            // ========================================================
            // LOG SUPPORT USERS
            // ========================================================

            _logger.LogInformation(
                "Found {Count} assigned support user(s) " +
                "for ClientId {ClientId}, TicketNo {TicketNo}.",
                supportEmails.Count,
                clientId,
                ticket.TicketNo);

            foreach (var supportUser in supportUsers)
            {
                _logger.LogInformation(
                    "Assigned Support: " +
                    "UserId={UserId}, Username={Username}, " +
                    "FullName={FullName}, Email={Email}",
                    supportUser.UserId,
                    supportUser.Username,
                    supportUser.FullName,
                    supportUser.EmailAddress);
            }

            // ========================================================
            // CALCULATE WAITING TIME
            // ========================================================

            var waitingTime = DateTime.UtcNow - clientMessageDate;

            var waitingHours = (int)Math.Floor(waitingTime.TotalHours);
            var waitingMinutes = waitingTime.Minutes;

            // ========================================================
            // HTML ENCODE
            // ========================================================

            var encodedMessage =
                System.Net.WebUtility.HtmlEncode(
                    lastClientMessage.Content);

            var encodedTicketNo =
                System.Net.WebUtility.HtmlEncode(
                    ticket.TicketNo);

            // ========================================================
            // EMAIL BODY
            // ========================================================

var emailBody = $@"
<!DOCTYPE html>
<html>

<head>
    <meta charset='UTF-8'>
    <title>ChatUp Support Reminder</title>
</head>

<body style='
    font-family: Arial, sans-serif;
    background-color: #f4f6f8;
    padding: 20px;
    margin: 0;
'>

    <div style='
        max-width: 600px;
        margin: auto;
        background: #ffffff;
        border-radius: 10px;
        padding: 25px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    '>

        <h2 style='
            color: #1877f2;
            margin-top: 0;
        '>
            ChatUp Support Reminder
        </h2>

        <p>
            A client has been waiting for a support response for
            <strong>{waitingHours} hour(s) {waitingMinutes} minute(s)</strong>.
        </p>

        <hr style='
            border: 0;
            border-top: 1px solid #e5e7eb;
            margin: 20px 0;
        '>

        <p>
            <strong>Ticket No:</strong>
            {encodedTicketNo}
        </p>

        <p>
            <strong>Client ID:</strong>
            {clientIdDisplay}
        </p>

        <p>
            <strong>Client Message:</strong>
        </p>

        <div style='
            background-color: #f1f3f5;
            padding: 15px;
            border-radius: 8px;
            margin: 10px 0 20px 0;
            white-space: pre-wrap;
        '>
            {encodedMessage}
        </div>

        <p>
            <strong>Message Time:</strong>
            {clientMessageDate:MMM dd, yyyy hh:mm:ss tt} UTC
        </p>

        <p>
            <strong>Reminder Threshold:</strong>
            {_settings.ReminderMinutes} minute(s)
        </p>

        <br>

        <a
            href='https://portal.odeccisolutions.com/'
            style='
                background-color: #1877f2;
                color: #ffffff;
                padding: 12px 20px;
                border-radius: 6px;
                text-decoration: none;
                display: inline-block;
            '>
            Open Ticket
        </a>

        <br><br>

        <p style='
            font-size: 12px;
            color: #999999;
        '>
            This is an automated notification from ChatUp.
        </p>

    </div>

</body>

</html>";


            // ========================================================
            // SEND EMAIL
            // ========================================================

            var successfullySent = false;

            foreach (var supportEmail in supportEmails)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    _logger.LogInformation(
                        "Sending support reminder to {Email} " +
                        "for Ticket {TicketNo}.",
                        supportEmail,
                        ticket.TicketNo);

                    await emailService.SendEmailAsync(
                        supportEmail,
                        $"Action Required: Ticket #{ticket.TicketNo} Awaiting Response",
                        emailBody,
                        true);

                    successfullySent = true;

                    _logger.LogInformation(
                        "Support reminder successfully sent to {Email} " +
                        "for Ticket {TicketNo}.",
                        supportEmail,
                        ticket.TicketNo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed sending support reminder to {Email} " +
                        "for Ticket {TicketNo}.",
                        supportEmail,
                        ticket.TicketNo);
                }
            }

            // ========================================================
            // MARK REMINDER AS SENT
            // ========================================================

            if (successfullySent)
            {
                ticket.SupportResponseReminderSent =
                    true;

                ticket.SupportResponseReminderSentAt =
                    DateTime.UtcNow;

                _logger.LogInformation(
                    "Support response reminder marked as sent. " +
                    "TicketNo: {TicketNo}",
                    ticket.TicketNo);
            }
            else
            {
                _logger.LogWarning(
                    "No support reminder email was successfully sent " +
                    "for Ticket {TicketNo}. " +
                    "The ticket will be retried on the next check.",
                    ticket.TicketNo);
            }
        }
    }
}