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
            Status = OutboxMessageStatus.Pending
        };
    }

    public void MarkPublished(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Status = OutboxMessageStatus.Published;
        PublishedAt = timeProvider.GetUtcNow();
        LastError = null;
    }

    public void MarkAttemptFailed(string error)
    {
        AttemptCount++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error.Trim();
        if (AttemptCount >= 10)
        {
            Status = OutboxMessageStatus.Failed;
        }
    }
}
