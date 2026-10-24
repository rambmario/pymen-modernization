using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Pymen.Domain.Events;
using Pymen.Infrastructure.Messaging;

namespace Pymen.Worker;

public sealed class DomainEventsConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DomainEventsConsumer> _logger;

    public DomainEventsConsumer(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<DomainEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _processor = client.CreateProcessor(
            options.Value.QueueName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1,
                PrefetchCount = 4
            });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Domain events consumer started on queue.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown
        }
        finally
        {
            await _processor.StopProcessingAsync();
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        string? subject = args.Message.Subject;
        if (string.IsNullOrWhiteSpace(subject)
            && args.Message.ApplicationProperties.TryGetValue("EventType", out var eventType))
        {
            subject = eventType?.ToString();
        }

        _logger.LogInformation(
            "Received Service Bus message {MessageId} subject {Subject}",
            messageId,
            subject);

        try
        {
            if (subject == typeof(WorkOrderDelivered).FullName
                || subject?.EndsWith(nameof(WorkOrderDelivered), StringComparison.Ordinal) == true)
            {
                var payload = args.Message.Body.ToString();
                var domainEvent = JsonSerializer.Deserialize<WorkOrderDelivered>(payload, JsonOptions)
                    ?? throw new InvalidOperationException("Invalid WorkOrderDelivered payload.");

                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<WorkOrderDeliveredPdfHandler>();
                await handler.HandleAsync(domainEvent, messageId, args.CancellationToken);
            }
            else
            {
                _logger.LogInformation("Ignoring unhandled event type {Subject}", subject);
            }

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing message {MessageId}; abandoning for retry/DLQ", messageId);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus processor error source={ErrorSource} entity={EntityPath}",
            args.ErrorSource,
            args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
