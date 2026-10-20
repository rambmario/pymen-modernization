using FluentAssertions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Common;
using Pymen.Domain.Inventory;
using Pymen.Domain.Production;

namespace Pymen.Domain.Tests.Production;

public class ProductionReceiptAndDeliveryTests
{
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

        var receipt = ProductionReceipt.Create(tenantId, finishedId, warehouseId, quantity: 5);

        receipt.Apply(
            finishedStock,
            [bom],
            new Dictionary<Guid, StockBalance> { [rawId] = rawStock });

        finishedStock.Quantity.Should().Be(5);
        rawStock.Quantity.Should().Be(90);
        receipt.IsApplied.Should().BeTrue();
    }

    [Fact]
    public void ProductionReceipt_Apply_twice_throws()
    {
        var tenantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var finishedId = Guid.NewGuid();
        var finishedStock = StockBalance.Create(tenantId, finishedId, warehouseId, 0);
        var receipt = ProductionReceipt.Create(tenantId, finishedId, warehouseId, 1);

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

        var note = DeliveryNote.Create(tenantId, Guid.NewGuid(), "DN-0001");
        note.AddLine(productId, 10);

        note.Lines.Should().HaveCount(1);
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
        var receipt = ProductionReceipt.Create(tenantId, finishedId, warehouseId, 1);

        var act = () => receipt.Apply(
            finishedStock,
            [bom],
            new Dictionary<Guid, StockBalance> { [rawId] = rawStock });

        act.Should().Throw<DomainException>().WithMessage("*Insufficient stock*");
    }
}
