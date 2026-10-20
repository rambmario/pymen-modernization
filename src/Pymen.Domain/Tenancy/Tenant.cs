using Pymen.Domain.Common;

namespace Pymen.Domain.Tenancy;

public sealed class Tenant : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string PointOfSaleCode { get; private set; } = string.Empty;
    public int NextWorkOrderSequence { get; private set; } = 1;
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Optimistic concurrency token for safe work-order numbering under parallel allocates.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private Tenant()
    {
    }

    public static Tenant Create(string name, string pointOfSaleCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tenant name is required.");
        }

        if (string.IsNullOrWhiteSpace(pointOfSaleCode))
        {
            throw new DomainException("Point of sale code is required.");
        }

        var normalizedPos = pointOfSaleCode.Trim();
        if (normalizedPos.Length is < 1 or > 8)
        {
            throw new DomainException("Point of sale code must be between 1 and 8 characters.");
        }

        return new Tenant
        {
            Name = name.Trim(),
            PointOfSaleCode = normalizedPos.PadLeft(4, '0'),
            NextWorkOrderSequence = 1,
            IsActive = true
        };
    }

    /// <summary>
    /// Allocates the next work-order number for this tenant: POS (4) + sequence (8).
    /// </summary>
    public string AllocateWorkOrderNumber()
    {
        if (!IsActive)
        {
            throw new DomainException("Cannot allocate work order numbers for an inactive tenant.");
        }

        var number = $"{PointOfSaleCode}{NextWorkOrderSequence.ToString().PadLeft(8, '0')}";
        NextWorkOrderSequence++;
        return number;
    }
}
