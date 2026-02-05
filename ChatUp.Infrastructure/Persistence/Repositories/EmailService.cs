using ChatUp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ChatUp.Infrastructure.Persistence.Repositories
{
    public class EmailService : IEmailService
    {
        IConfiguration config = new ConfigurationBuilder()
      .SetBasePath(Path.GetPathRoot(Environment.SystemDirectory))
      .AddJsonFile("app/chatup/appconfig.json", optional: true, reloadOnChange: true)
      .Build();
        private readonly ILogger<EmailService> _logger;

        public EmailService( ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpHost = config["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(config["EmailSettings:SmtpPort"]);
                var smtpUser = config["EmailSettings:SmtpUser"];
                var smtpPass = config["EmailSettings:SmtpPass"];
                var from = config["EmailSettings:From"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                using var message = new MailMessage(from, to, subject, body)
                {
                    IsBodyHtml = isHtml
                };

                _logger.LogInformation("[EmailService] Sending email to {Email}", to);
                await client.SendMailAsync(message);
                _logger.LogInformation("[EmailService] Email sent successfully to {Email}", to);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Failed to send email to {Email}", to);
                return false;
            }
        }

    }
}
