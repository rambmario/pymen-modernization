using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Pymen.Domain.Catalog;
using Pymen.Domain.Common;
using Pymen.Domain.Inventory;
using Pymen.Domain.Production;

namespace Pymen.Domain.Tests.Production;

public class ProductionReceiptAndDeliveryTests
{
    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ProductionReceipt_moves_stock_using_BOM()
    {
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var finishedId = Guid.NewGuid();
        var rawId = Guid.NewGuid();

        var bom = BillOfMaterialsLine.Create(tenantId, finishedId, rawId, quantityPerUnit: 2m);
        var finishedStock = StockBalance.Create(tenantId, finishedId, warehouseId, 0);
        var rawStock = StockBalance.Create(tenantId, rawId, warehouseId, 100);

        var receipt = ProductionReceipt.Create(tenantId, finishedId, warehouseId, quantity: 5, Time);

        receipt.Apply(
            finishedStock,
            [bom],
            new Dictionary<Guid, StockBalance> { [rawId] = rawStock });

        finishedStock.Quantity.Should().Be(5);
        rawStock.Quantity.Should().Be(90);
        receipt.IsApplied.Should().BeTrue();
        receipt.ReceivedAt.Should().Be(Time.GetUtcNow());
    }

    [Fact]
    public void ProductionReceipt_Apply_twice_throws()
    {
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var finishedId = Guid.NewGuid();
        var finishedStock = StockBalance.Create(tenantId, finishedId, warehouseId, 0);
        var receipt = ProductionReceipt.Create(tenantId, finishedId, warehouseId, 1, Time);

        receipt.Apply(finishedStock, [], new Dictionary<Guid, StockBalance>());

        var act = () => receipt.Apply(finishedStock, [], new Dictionary<Guid, StockBalance>());

        act.Should().Throw<DomainException>().WithMessage("*already applied*");
    }

    [Fact]
    public void DeliveryNote_documents_lines_without_touching_stock()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var stock = StockBalance.Create(tenantId, productId, warehouseId, 50);

        var note = DeliveryNote.Create(tenantId, Guid.NewGuid(), "DN-0001", Time);
        note.AddLine(productId, 10);

        note.Lines.Should().HaveCount(1);
        note.DeliveredAt.Should().Be(Time.GetUtcNow());
        stock.Quantity.Should().Be(50);
    }

    [Fact]
    public void Insufficient_raw_material_throws()
    {
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var finishedId = Guid.NewGuid();
        var rawId = Guid.NewGuid();
        var bom = BillOfMaterialsLine.Create(tenantId, finishedId, rawId, 10m);
        var finishedStock = StockBalance.Create(tenantId, finishedId, warehouseId, 0);
        var rawStock = StockBalance.Create(tenantId, rawId, warehouseId, 5);
        var receipt = ProductionReceipt.Create(tenantId, finishedId, warehouseId, 1, Time);

        var act = () => receipt.Apply(
            finishedStock,
            [bom],
            new Dictionary<Guid, StockBalance> { [rawId] = rawStock });

        act.Should().Throw<DomainException>().WithMessage("*Insufficient stock*");
    }
}
