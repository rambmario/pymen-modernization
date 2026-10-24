using Azure.Core;
using Azure.Messaging.ServiceBus;
using Pymen.Infrastructure.Azure;

namespace Pymen.Infrastructure.Messaging;

/// <summary>
/// Dual-mode Service Bus client: connection string (local emulator) or
/// FullyQualifiedNamespace + DefaultAzureCredential (Azure Managed Identity).
/// </summary>
public static class ServiceBusClientFactory
{
    public static bool IsConfigured(ServiceBusOptions options) =>
        !string.IsNullOrWhiteSpace(options.ConnectionString)
        || !string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace);

    public static ServiceBusClient Create(ServiceBusOptions options, TokenCredential? credential = null)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new ServiceBusClient(options.ConnectionString);
        }

        if (!string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
        {
            return new ServiceBusClient(
                options.FullyQualifiedNamespace,
                credential ?? AzureCredentialFactory.Create());
        }

        throw new InvalidOperationException(
            "Configure ServiceBus:ConnectionString (local emulator) or ServiceBus:FullyQualifiedNamespace (Azure MI).");
    }
}
