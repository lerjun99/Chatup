using System.Threading.Channels;
using ChatUp.Application.Auditing;
using ChatUp.Domain.Entities;
using ChatUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatUp.Infrastructure.Auditing
{
    /// <summary>In-memory buffer between requests and <see cref="ActivityLogWriter"/>.</summary>
    public sealed class ActivityLogQueue : IActivityLogQueue
    {
        private readonly ILogger<ActivityLogQueue> _logger;

        private readonly Channel<AdminActivityLog> _channel = Channel.CreateBounded<AdminActivityLog>(new BoundedChannelOptions(20_000)
        {
            FullMode = BoundedChannelFullMode.Wait, // TryWrite returns false when full instead of blocking
            SingleReader = true,
            SingleWriter = false
        });

        public ActivityLogQueue(ILogger<ActivityLogQueue> logger) => _logger = logger;

        public ChannelReader<AdminActivityLog> Reader => _channel.Reader;

        public bool Enqueue(AdminActivityLog log)
        {
            if (_channel.Writer.TryWrite(log))
                return true;

            // Keep the row in the application log rather than losing it silently
            _logger.LogWarning("Audit queue full, row not stored: {Category} {Action} {Module} {User} {Summary}",
                log.Category, log.Action, log.Module, log.UserName, log.Summary);
            return false;
        }
    }

    /// <summary>Writes queued audit rows to the database in batches, off the request thread.</summary>
    public sealed class ActivityLogWriter : BackgroundService
    {
        private const int BatchSize = 200;

        private readonly ActivityLogQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ActivityLogWriter> _logger;

        public ActivityLogWriter(ActivityLogQueue queue, IServiceScopeFactory scopeFactory, ILogger<ActivityLogWriter> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await _queue.Reader.WaitToReadAsync(stoppingToken))
                    await WriteAvailableAsync();
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }

            // Flush what's still buffered before the process exits
            await WriteAvailableAsync();
        }

        private async Task WriteAvailableAsync()
        {
            var batch = new List<AdminActivityLog>(BatchSize);

            while (_queue.Reader.TryRead(out var row))
            {
                batch.Add(row);
                if (batch.Count == BatchSize)
                {
                    await WriteBatchAsync(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await WriteBatchAsync(batch);
        }

        private async Task WriteBatchAsync(List<AdminActivityLog> batch)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ChatDBContext>();

                db.AdminActivityLogs.AddRange(batch);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log: {Count} rows could not be written", batch.Count);
                foreach (var row in batch)
                    _logger.LogWarning("Audit row not stored: {Category} {Action} {Module} {User} {EntityId} {Summary}",
                        row.Category, row.Action, row.Module, row.UserName, row.EntityId, row.Summary);
            }
        }
    }

    /// <summary>
    /// Daily clean-up. Page visits are high volume and only useful for recent questions, so they're kept for
    /// ActivityLog:PageViewRetentionDays (default 90). Everything else for ActivityLog:RetentionDays (default 0 = forever).
    /// </summary>
    public sealed class ActivityLogRetentionService : BackgroundService
    {
        private const int DeleteBatch = 5_000;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<ActivityLogRetentionService> _logger;

        public ActivityLogRetentionService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<ActivityLogRetentionService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                await PurgeAsync(stoppingToken);

                try { await Task.Delay(TimeSpan.FromHours(24), stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }

        private async Task PurgeAsync(CancellationToken ct)
        {
            try
            {
                var pageViewDays = _config.GetValue("ActivityLog:PageViewRetentionDays", 90);
                var retentionDays = _config.GetValue("ActivityLog:RetentionDays", 0);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ChatDBContext>();
                var removed = 0;

                if (pageViewDays > 0)
                {
                    var cutoff = DateTime.UtcNow.AddDays(-pageViewDays);
                    removed += await DeleteInBatchesAsync(db.AdminActivityLogs.Where(x => x.Category == ActivityCategories.Navigation && x.OccurredAt < cutoff), ct);
                }

                if (retentionDays > 0)
                {
                    var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
                    removed += await DeleteInBatchesAsync(db.AdminActivityLogs.Where(x => x.OccurredAt < cutoff), ct);
                }

                if (removed > 0)
                    _logger.LogInformation("Audit log retention removed {Count} rows", removed);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log retention failed");
            }
        }

        private static async Task<int> DeleteInBatchesAsync(IQueryable<AdminActivityLog> query, CancellationToken ct)
        {
            var total = 0;
            int deleted;
            do
            {
                deleted = await query.OrderBy(x => x.Id).Take(DeleteBatch).ExecuteDeleteAsync(ct);
                total += deleted;
            }
            while (deleted == DeleteBatch && !ct.IsCancellationRequested);

            return total;
        }
    }
}
