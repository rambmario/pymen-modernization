using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pymen.Domain.Events;
using Pymen.Infrastructure.Documents;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Storage;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Infrastructure.Messaging;

public sealed class WorkOrderDeliveredPdfHandler
{
    private readonly PymenDbContext _db;
    private readonly AmbientTenantContext _tenantContext;
    private readonly IDeliveryNotePdfGenerator _pdfGenerator;
    private readonly IDeliveryNotePdfStore _pdfStore;
    private readonly TimeProvider _time;
    private readonly ILogger<WorkOrderDeliveredPdfHandler> _logger;

    public WorkOrderDeliveredPdfHandler(
        PymenDbContext db,
        AmbientTenantContext tenantContext,
        IDeliveryNotePdfGenerator pdfGenerator,
        IDeliveryNotePdfStore pdfStore,
        TimeProvider time,
        ILogger<WorkOrderDeliveredPdfHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _pdfGenerator = pdfGenerator;
        _pdfStore = pdfStore;
        _time = time;
        _logger = logger;
    }

    public async Task HandleAsync(
        WorkOrderDelivered domainEvent,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (await _db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken))
        {
            _logger.LogInformation("Skipping already processed message {MessageId}", messageId);
            return;
        }

        if (await _pdfStore.ExistsAsync(domainEvent.TenantId, domainEvent.WorkOrderId, cancellationToken))
        {
            _logger.LogInformation(
                "PDF already exists for work order {WorkOrderId}; recording message {MessageId}",
                domainEvent.WorkOrderId,
                messageId);
            _db.ProcessedMessages.Add(ProcessedMessage.Create(messageId, typeof(WorkOrderDelivered).FullName!, _time));
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _tenantContext.SetTenant(domainEvent.TenantId);
        try
        {
            var workOrder = await _db.WorkOrders
                .Include(w => w.Lines)
                .FirstOrDefaultAsync(w => w.Id == domainEvent.WorkOrderId, cancellationToken)
                ?? throw new InvalidOperationException($"Work order {domainEvent.WorkOrderId} not found.");

            var tenant = await _db.Tenants.FirstAsync(t => t.Id == domainEvent.TenantId, cancellationToken);
            var customer = await _db.Customers.FirstAsync(c => c.Id == workOrder.CustomerId, cancellationToken);

            var note = await _db.DeliveryNotes
                .Include(d => d.Lines)
                .FirstOrDefaultAsync(d => d.WorkOrderId == workOrder.Id, cancellationToken);

            var productIds = note is null
                ? workOrder.Lines.Select(l => l.ProductId).Distinct().ToList()
                : note.Lines.Select(l => l.ProductId).Distinct().ToList();

            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var lines = note is null
                ? workOrder.Lines.OrderBy(l => l.LineNumber).Select(l => ToPdfLine(l.LineNumber, l.ProductId, l.Quantity, products)).ToList()
                : note.Lines.OrderBy(l => l.LineNumber).Select(l => ToPdfLine(l.LineNumber, l.ProductId, l.Quantity, products)).ToList();

            var model = new DeliveryNotePdfModel(
                tenant.Name,
                note?.Number ?? domainEvent.DeliveryNoteNumber,
                workOrder.Number,
                customer.Name,
                note?.DeliveredAt ?? domainEvent.OccurredAt,
                lines);

            var pdf = _pdfGenerator.Generate(model);
            await _pdfStore.UploadAsync(domainEvent.TenantId, domainEvent.WorkOrderId, pdf, cancellationToken);

            _db.ProcessedMessages.Add(ProcessedMessage.Create(messageId, typeof(WorkOrderDelivered).FullName!, _time));
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Generated delivery-note PDF for work order {WorkOrderId} remito {DeliveryNoteNumber} (message {MessageId})",
                domainEvent.WorkOrderId,
                model.DeliveryNoteNumber,
                messageId);
        }
        finally
        {
            _tenantContext.Clear();
        }
    }

    private static DeliveryNotePdfLine ToPdfLine(
        int lineNumber,
        Guid productId,
        decimal quantity,
        IReadOnlyDictionary<Guid, Domain.Catalog.Product> products)
    {
        products.TryGetValue(productId, out var product);
        return new DeliveryNotePdfLine(
            lineNumber,
            product?.Sku ?? "—",
            product?.Name ?? "—",
            quantity);
    }
}
