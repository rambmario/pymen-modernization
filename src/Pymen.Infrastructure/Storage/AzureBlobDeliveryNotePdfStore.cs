using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Pymen.Infrastructure.Storage;

public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    /// <summary>Azurite / shared key. Prefer BlobServiceUri + MI in Azure.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>e.g. https://stpymendemo….blob.core.windows.net/ (Azure MI mode).</summary>
    public string? BlobServiceUri { get; set; }

    public string DeliveryNotesContainer { get; set; } = "delivery-notes";
}

public sealed class AzureBlobDeliveryNotePdfStore : IDeliveryNotePdfStore
{
    private readonly BlobContainerClient _container;

    public AzureBlobDeliveryNotePdfStore(IOptions<AzureStorageOptions> options)
        : this(BlobServiceClientFactory.Create(options.Value), options.Value.DeliveryNotesContainer)
    {
    }

    internal AzureBlobDeliveryNotePdfStore(BlobServiceClient service, string containerName)
    {
        _container = service.GetBlobContainerClient(containerName);
    }

    public async Task UploadAsync(Guid tenantId, Guid workOrderId, byte[] pdf, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        var blob = _container.GetBlobClient(BlobPath(tenantId, workOrderId));
        await using var stream = new MemoryStream(pdf, writable: false);
        await blob.UploadAsync(stream, overwrite: true, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(BlobPath(tenantId, workOrderId));
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task<bool> ExistsAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(BlobPath(tenantId, workOrderId));
        return await blob.ExistsAsync(cancellationToken);
    }

    internal static string BlobPath(Guid tenantId, Guid workOrderId) =>
        $"{tenantId:N}/{workOrderId:N}.pdf";
}
