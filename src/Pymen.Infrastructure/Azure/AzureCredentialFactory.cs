using Azure.Core;
using Azure.Identity;

namespace Pymen.Infrastructure.Azure;

/// <summary>
/// Builds the credential used in Azure (Managed Identity via DefaultAzureCredential).
/// Local emulators use connection strings instead — see ServiceBus / Storage factories.
/// </summary>
public static class AzureCredentialFactory
{
    public static TokenCredential Create() =>
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // When AZURE_CLIENT_ID is set (ACA user-assigned MI), ManagedIdentityCredential uses it.
            ExcludeInteractiveBrowserCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeAzureCliCredential = false,
            ExcludeManagedIdentityCredential = false,
        });
}
