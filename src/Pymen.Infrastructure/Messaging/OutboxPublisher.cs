using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Infrastructure.Messaging;

/// <summary>
/// Publishes pending Outbox rows to Service Bus.
/// Claims rows with UPDLOCK/READPAST so multiple API replicas never send the same MessageId.
/// </summary>
public sealed class OutboxPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxMessageSender _sender;
    private readonly TimeProvider _time;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly string _instanceId;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IOutboxMessageSender sender,
        TimeProvider time,
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisher> logger,
        string? instanceId = null)
    {
        _scopeFactory = scopeFactory;
        _sender = sender;
        _time = time;
        _options = options.Value;
        _logger = logger;
        _instanceId = string.IsNullOrWhiteSpace(instanceId)
            ? $"{Environment.MachineName}:{Guid.CreateVersion7():N}"
            : instanceId;
    }

    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();

        var claimed = await ClaimBatchAsync(db, cancellationToken);
        if (claimed.Count == 0)
        {
            return 0;
        }

        var published = 0;
        foreach (var message in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _sender.SendAsync(message, cancellationToken);
                message.MarkPublished(_time);
                await db.SaveChangesAsync(cancellationToken);
                published++;
            }
            catch (Exception ex)
            {
                var nextAttempt = message.AttemptCount + 1;
                _logger.LogError(
                    ex,
                    "Failed to publish outbox message {MessageId} (attempt {Attempt}/{MaxAttempts})",
                    message.Id,
                    nextAttempt,
                    _options.MaxAttempts);

                message.MarkAttemptFailed(
                    ex.Message,
                    _time,
                    _options.MaxAttempts,
                    _options.MaxBackoffSeconds);

                if (message.Status == OutboxMessageStatus.Failed)
                {
                    _logger.LogError(
                        "Outbox message {MessageId} moved to Failed after {Attempts} attempts. LastError={LastError}",
                        message.Id,
                        message.AttemptCount,
                        message.LastError);
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return published;
    }

    private async Task<List<OutboxMessage>> ClaimBatchAsync(
        PymenDbContext db,
        CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var lockUntil = now.AddSeconds(Math.Max(5, _options.LockDurationSeconds));
        var batchSize = Math.Max(1, _options.BatchSize);

        // SQL Server: UPDLOCK + READPAST skips rows locked by other replicas; OUTPUT returns claimed rows.
        var sql = """
            ;WITH cte AS (
                SELECT TOP (@batchSize) *
                FROM OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = N'Pending'
                  AND (LockedUntil IS NULL OR LockedUntil < @now)
                  AND (NextAttemptAt IS NULL OR NextAttemptAt <= @now)
                ORDER BY OccurredAt
            )
            UPDATE cte
            SET LockedUntil = @lockUntil,
                LockedBy = @lockedBy
            OUTPUT INSERTED.*;
            """;

        return await db.OutboxMessages
            .FromSqlRaw(
                sql,
                new SqlParameter("@batchSize", batchSize),
                new SqlParameter("@now", now),
                new SqlParameter("@lockUntil", lockUntil),
                new SqlParameter("@lockedBy", _instanceId))
            .AsTracking()
            .ToListAsync(cancellationToken);
    }
}
