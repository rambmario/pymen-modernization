namespace Pymen.Infrastructure.Messaging;

public interface IOutboxMessageSender
{
    Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
