namespace Pymen.Infrastructure.Messaging;

/// <summary>
/// Consumer-side idempotency record (Service Bus Basic has no broker duplicate detection).
/// </summary>
public sealed class ProcessedMessage
{
    public string MessageId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedMessage()
    {
    }

    public static ProcessedMessage Create(string messageId, string eventType, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentNullException.ThrowIfNull(timeProvider);

        return new ProcessedMessage
        {
            MessageId = messageId.Trim(),
            EventType = eventType.Trim(),
            ProcessedAt = timeProvider.GetUtcNow()
        };
    }
}
