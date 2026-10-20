using Pymen.Domain.Common;

namespace Pymen.Domain.Production;

/// <summary>
/// Documents delivery of a work order. Does not move stock.
/// </summary>
public sealed class DeliveryNote : Entity, ITenantOwned
{
    private readonly List<DeliveryNoteLine> _lines = [];

    public Guid TenantId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public DateTimeOffset DeliveredAt { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<DeliveryNoteLine> Lines => _lines.AsReadOnly();

    private DeliveryNote()
    {
    }

    public static DeliveryNote Create(
        Guid tenantId,
        Guid workOrderId,
        string number,
        TimeProvider timeProvider,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (tenantId == Guid.Empty)
        {
            throw new DomainException("TenantId is required.");
        }

        if (workOrderId == Guid.Empty)
        {
            throw new DomainException("WorkOrderId is required.");
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            throw new DomainException("Delivery note number is required.");
        }

        return new DeliveryNote
        {
            TenantId = tenantId,
            WorkOrderId = workOrderId,
            Number = number.Trim(),
            DeliveredAt = timeProvider.GetUtcNow(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public DeliveryNoteLine AddLine(Guid productId, decimal quantity)
    {
        var line = DeliveryNoteLine.Create(TenantId, Id, productId, quantity, _lines.Count + 1);
        _lines.Add(line);
        return line;
    }
}
