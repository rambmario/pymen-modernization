namespace Pymen.Infrastructure.Storage;

public interface IDeliveryNotePdfStore
{
    Task UploadAsync(Guid tenantId, Guid workOrderId, byte[] pdf, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default);
}
