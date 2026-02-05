using ChatUp.Application.Features.Ticket.DTOs;
using ChatUp.Domain.Entities;

namespace ChatUp.Services
{
    public class TicketNotificationService
    {
        private readonly EmailService _email;
        public enum TicketNotificationType
        {
            NewTicket,
            ClosedTicket,
            ReOpened,
            Assigned,
            Updated
        }
        public TicketNotificationService(EmailService email)
        {
            _email = email;
        }


        public async Task SendAsync(TicketDto ticket, TicketNotificationType type, string? remarks)
        { 
            // 🔹 Send to requester
            await SendToRequester(ticket, type, remarks);

            // 🔹 Send to assigned developer
            await SendToDeveloper(ticket, type, remarks);
        }
        private async Task SendToRequester(TicketDto ticket, TicketNotificationType type, string? remarks)
        {
            string subject = BuildSubject(ticket, type, isDeveloper: false);
            string body = BuildBody(ticket, type, remarks, isDeveloper: false);

            await _email.SendEmailAsync(ticket.RequesterEmail, subject, body);
        }

        private async Task SendToDeveloper(TicketDto ticket, TicketNotificationType type, string? remarks)
        {
            if (string.IsNullOrWhiteSpace(ticket.DeveloperEmail))
                return; // no developer assigned

            string subject = BuildSubject(ticket, type, isDeveloper: true);
            string body = BuildBody(ticket, type, remarks, isDeveloper: true);

            await _email.SendEmailAsync(ticket.DeveloperEmail, subject, body);
        }

        private string BuildSubject(TicketDto ticket, TicketNotificationType type, bool isDeveloper)
        {
            if (isDeveloper)
            {
                return type switch
                {
                    TicketNotificationType.Assigned =>
                        $"🛠 You have been assigned Ticket #{ticket.TicketNo}: {ticket.IssueTitle}",

                    TicketNotificationType.Updated =>
                        $"🔧 Ticket Assigned to You Updated: {ticket.IssueTitle}",

                    _ => $"Ticket Update: #{ticket.TicketNo}"
                };
            }

            // Requester
            return type switch
            {
                TicketNotificationType.NewTicket =>
                    $"🎫 New Ticket Created: #{ticket.TicketNo} - {ticket.IssueTitle}",

                TicketNotificationType.Assigned =>
                    $"[Ticket Update] {ticket.IssueTitle} - Assigned to Developer",

                TicketNotificationType.ClosedTicket =>
                    $"✔ Ticket Closed: #{ticket.TicketNo}",

                _ => "Ticket Notification"
            };
        }

        private string BuildBody(TicketDto ticket, TicketNotificationType type, string? remarks, bool isDeveloper)
        {
            string header = type switch
            {
                TicketNotificationType.NewTicket => "New Ticket Created",
                TicketNotificationType.Assigned => isDeveloper ? "You Have Been Assigned a Ticket" : "Ticket Assigned",
                TicketNotificationType.ClosedTicket => "Ticket Closed",
                TicketNotificationType.Updated => "Ticket Update",
                _ => "Ticket Update"
            };

            string greetName = isDeveloper ? ticket.DeveloperName : ticket.RequestedByName;

            // Map TicketPriority to HTML badge color
            string priorityBadge = ticket.Priority switch
            {
                TicketPriority.Critical => "<span style='background:#343a40;color:white;padding:2px 6px;border-radius:4px;'>Critical</span>",
                TicketPriority.High => "<span style='background:#dc3545;color:white;padding:2px 6px;border-radius:4px;'>High</span>",
                TicketPriority.Medium => "<span style='background:#ffc107;color:#212529;padding:2px 6px;border-radius:4px;'>Medium</span>",
                TicketPriority.Low => "<span style='background:#28a745;color:white;padding:2px 6px;border-radius:4px;'>Low</span>",
                _ => "<span style='background:#6c757d;color:white;padding:2px 6px;border-radius:4px;'>Unknown</span>"
            };

            string remarksBlock = !string.IsNullOrEmpty(remarks)
                ? $@"<tr>
               <td style='font-weight:bold;display: flex;'>Remarks:</td>
               <td>{remarks}</td>
           </tr>"
                : "";

            return $@"
        <html>
        <body style='font-family:Segoe UI,Roboto,Arial; background:#f5f5f5; padding:20px;'>
        <table style='max-width:600px; margin:auto; background:white; border-radius:8px; padding:20px;'>

        <tr><td>
        <h2 style='color:#d4a017;'>{header}</h2>

        <p>Hi <strong>{greetName}</strong>,</p>
        <p>{(isDeveloper ? "You have a new ticket assigned to you:" : "Your ticket has been updated:")}</p>

        <table cellpadding='6' cellspacing='0' style='width:100%; border-collapse:collapse;'>

        <tr><td style='font-weight:bold;width: 100px;'>Ticket No:</td><td>#{ticket.TicketNo}</td></tr>
        <tr><td style='font-weight:bold;'>Title:</td><td>{ticket.IssueTitle}</td></tr>
        <tr><td style='font-weight:bold;'>Status:</td><td>{ticket.Status}</td></tr>
        <tr><td style='font-weight:bold;'>Priority:</td><td>{priorityBadge}</td></tr>
       <tr><td style='font-weight:bold;'>Project:</td><td>{ticket.ProjectName}</td></tr>

        {remarksBlock}

 
        </table>

        <p style='margin-top:30px;'>Regards,<br/><strong>Odecci Support Team</strong></p>
        <span style='font-size:12px; color:#888;'>This is an automated email.</span>

        </td></tr>
        </table>
        </body>
        </html>";
        }
    }
}
