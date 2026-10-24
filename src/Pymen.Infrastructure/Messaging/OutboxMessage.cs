using System.Text.Json;
using Pymen.Domain.Common;

namespace Pymen.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public Guid? TenantId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;
    public DateTimeOffset? PublishedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public string? LockedBy { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent, JsonSerializerOptions jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var type = domainEvent.GetType();
        var payload = JsonSerializer.Serialize(domainEvent, type, jsonOptions);
        Guid? tenantId = null;
        var tenantProp = type.GetProperty("TenantId");
        if (tenantProp?.GetValue(domainEvent) is Guid tenant)
        {
            tenantId = tenant;
        }

        return new OutboxMessage
        {
            Type = type.FullName ?? type.Name,
            Payload = payload,
            TenantId = tenantId,
            OccurredAt = domainEvent.OccurredAt,
            Status = OutboxMessageStatus.Pending,
            NextAttemptAt = domainEvent.OccurredAt
        };
    }

    public void MarkPublished(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Status = OutboxMessageStatus.Published;
        PublishedAt = timeProvider.GetUtcNow();
        LastError = null;
        LockedUntil = null;
        LockedBy = null;
        NextAttemptAt = null;
    }

    /// <summary>
    /// Records a failed publish attempt with exponential backoff. Marks Failed when max attempts is reached.
    /// </summary>
    public void MarkAttemptFailed(
        string error,
        TimeProvider timeProvider,
        int maxAttempts,
        int maxBackoffSeconds)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        AttemptCount++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error.Trim();
        LockedUntil = null;
        LockedBy = null;

        if (AttemptCount >= maxAttempts)
        {
            Status = OutboxMessageStatus.Failed;
            NextAttemptAt = null;
            return;
        }

        var delaySeconds = Math.Min(
            maxBackoffSeconds,
            (int)Math.Pow(2, AttemptCount));
        NextAttemptAt = timeProvider.GetUtcNow().AddSeconds(delaySeconds);
        Status = OutboxMessageStatus.Pending;
    }
}
