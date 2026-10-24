using Azure.Core;
using Azure.Storage.Blobs;
using Pymen.Infrastructure.Azure;

namespace Pymen.Infrastructure.Storage;

/// <summary>
/// Dual-mode Blob client: connection string (Azurite) or
/// BlobServiceUri + DefaultAzureCredential (Azure Managed Identity).
/// </summary>
public static class BlobServiceClientFactory
{
    public static bool IsConfigured(AzureStorageOptions options) =>
        !string.IsNullOrWhiteSpace(options.ConnectionString)
        || !string.IsNullOrWhiteSpace(options.BlobServiceUri);

    public static BlobServiceClient Create(AzureStorageOptions options, TokenCredential? credential = null)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new BlobServiceClient(options.ConnectionString);
        }

        if (!string.IsNullOrWhiteSpace(options.BlobServiceUri)
            && Uri.TryCreate(options.BlobServiceUri, UriKind.Absolute, out var uri))
        {
            return new BlobServiceClient(uri, credential ?? AzureCredentialFactory.Create());
        }

        throw new InvalidOperationException(
            "Configure AzureStorage:ConnectionString (Azurite) or AzureStorage:BlobServiceUri (Azure MI).");
    }
}
