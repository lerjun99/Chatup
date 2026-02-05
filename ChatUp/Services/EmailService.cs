using System.Net;
using System.Net.Mail;

namespace ChatUp.Services
{
    public class EmailService
    {
        IConfiguration config = new ConfigurationBuilder()
         .SetBasePath(Path.GetPathRoot(Environment.SystemDirectory))
         .AddJsonFile("app/chatup/appconfig.json", optional: true, reloadOnChange: true)
         .Build();
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
          
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
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
                IsBodyHtml = true
            };

            _logger.LogInformation($"[EmailService] Sending email to {to}...");
            await client.SendMailAsync(message);
            _logger.LogInformation($"[EmailService] Email sent successfully to {to}");
        }
    }
}
