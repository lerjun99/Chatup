namespace ChatUp.Services
{
    public class ContractExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContractExpiryBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1); // ✅ for testing

        public ContractExpiryBackgroundService(IServiceProvider serviceProvider, ILogger<ContractExpiryBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ContractExpiryBackgroundService] Started at: {time}", DateTime.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiringContractsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ContractExpiryBackgroundService] Error during expiry check");
                }

                _logger.LogInformation("[ContractExpiryBackgroundService] Waiting {0} before next check...", _checkInterval);
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckExpiringContractsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var contractService = scope.ServiceProvider.GetRequiredService<ContractService>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            var contracts = await contractService.GetAllAsync();
            var now = DateTime.UtcNow.Date;

            foreach (var contract in contracts)
            {
                if (!contract.ExpirationDate.HasValue)
                    continue;

                var expiryDate = contract.ExpirationDate.Value.Date;
                var daysUntilExpiry = (expiryDate - now).TotalDays;

                string? subject = null;
                string? body = null;

                // ✅ Case 1: 30-day reminder
                if (daysUntilExpiry == 30)
                {
                    subject = $"Contract Reminder: {contract.Title} expires in 30 days";
                    body = $@"
                        <p>Dear {contract.ClientName},</p>
                        <p>This is a friendly reminder that your contract <strong>{contract.Title}</strong> 
                        will expire on <strong>{expiryDate:MMMM dd, yyyy}</strong>.</p>
                        <p>Please review or renew your contract at your earliest convenience.</p>
                        <p>Regards,<br/>ChatUp System</p>";
                }

                // ✅ Case 2: 15-day urgent reminder
                else if (daysUntilExpiry == 15)
                {
                    subject = $"Urgent: Contract {contract.Title} expires in 15 days";
                    body = $@"
                        <p>Dear {contract.ClientName},</p>
                        <p>Your contract <strong>{contract.Title}</strong> will expire on 
                        <strong>{expiryDate:MMMM dd, yyyy}</strong>.</p>
                        <p>Please take immediate action to renew or extend it.</p>
                        <p>Regards,<br/>ChatUp System</p>";
                }

                // ✅ Case 3: Already expired
                else if (daysUntilExpiry < 0)
                {
                    subject = $"Contract Expired: {contract.Title}";
                    body = $@"
                        <p>Dear {contract.ClientName},</p>
                        <p>Your contract <strong>{contract.Title}</strong> expired on 
                        <strong>{expiryDate:MMMM dd, yyyy}</strong>.</p>
                        <p>Please contact us if you wish to renew the contract.</p>
                        <p>Regards,<br/>ChatUp System</p>";
                }

                // ✅ Send email if any condition met
                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    try
                    {
                        string to = string.IsNullOrWhiteSpace(contract.EmailAddress)
                            ? "admin@example.com" // fallback if client email missing
                            : contract.EmailAddress;

                        await emailService.SendEmailAsync(to, subject, body);
                        _logger.LogInformation($"[ContractExpiryBackgroundService] Email sent for contract '{contract.Title}' ({subject}) to {to}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[ContractExpiryBackgroundService] Failed to send email for contract {contract.Title}");
                    }
                }
            }
        }
    }
}
