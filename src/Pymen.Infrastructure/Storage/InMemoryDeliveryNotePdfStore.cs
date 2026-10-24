using System.Collections.Concurrent;

namespace Pymen.Infrastructure.Storage;

public sealed class InMemoryDeliveryNotePdfStore : IDeliveryNotePdfStore
{
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

    public Task UploadAsync(Guid tenantId, Guid workOrderId, byte[] pdf, CancellationToken cancellationToken = default)
    {
        _blobs[Key(tenantId, workOrderId)] = pdf;
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default)
    {
        if (!_blobs.TryGetValue(Key(tenantId, workOrderId), out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
    }

    public Task<bool> ExistsAsync(Guid tenantId, Guid workOrderId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_blobs.ContainsKey(Key(tenantId, workOrderId)));

    private static string Key(Guid tenantId, Guid workOrderId) => $"{tenantId:N}/{workOrderId:N}";
}
