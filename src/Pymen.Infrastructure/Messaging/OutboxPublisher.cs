using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Infrastructure.Messaging;

/// <summary>
/// Publishes pending Outbox rows to Service Bus. Idempotent: Published rows are never sent again.
/// </summary>
public sealed class OutboxPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxMessageSender _sender;
    private readonly TimeProvider _time;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IOutboxMessageSender sender,
        TimeProvider time,
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _sender = sender;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.OccurredAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var published = 0;
        foreach (var message in pending)
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
                _logger.LogError(
                    ex,
                    "Failed to publish outbox message {MessageId} (attempt {Attempt})",
                    message.Id,
                    message.AttemptCount + 1);
                message.MarkAttemptFailed(ex.Message);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return published;
    }
}
