using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pymen.Infrastructure.Messaging;

public sealed class ServiceBusOutboxMessageSender : IOutboxMessageSender, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusOutboxMessageSender> _logger;

    public ServiceBusOutboxMessageSender(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusOutboxMessageSender> logger)
    {
        _logger = logger;
        _sender = client.CreateSender(options.Value.QueueName);
    }

    public async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var busMessage = new ServiceBusMessage(message.Payload)
        {
            MessageId = message.Id.ToString("N"),
            ContentType = "application/json",
            Subject = message.Type
        };
        busMessage.ApplicationProperties["EventType"] = message.Type;
        if (message.TenantId is { } tenantId)
        {
            busMessage.ApplicationProperties["TenantId"] = tenantId.ToString("N");
        }

        await _sender.SendMessageAsync(busMessage, cancellationToken);
        _logger.LogInformation(
            "Published outbox message {MessageId} type {EventType}",
            message.Id,
            message.Type);
    }

    public async ValueTask DisposeAsync() => await _sender.DisposeAsync();
}
