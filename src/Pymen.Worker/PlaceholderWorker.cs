namespace Pymen.Worker;

/// <summary>
/// Host placeholder so the local Docker stack can start a worker container.
/// Paso 3 replaces this with Service Bus consumption and PDF generation.
/// </summary>
public sealed class PlaceholderWorker(ILogger<PlaceholderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Pymen.Worker host started (placeholder). Service Bus consumer arrives in Phase 2 / Paso 3.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
    }
}
