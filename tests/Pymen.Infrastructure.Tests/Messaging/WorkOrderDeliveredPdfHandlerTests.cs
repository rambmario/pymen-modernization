using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Pymen.Domain.Catalog;
using Pymen.Domain.Events;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;
using Pymen.Infrastructure.Documents;
using Pymen.Infrastructure.Messaging;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Storage;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Infrastructure.Tests.Messaging;

public class WorkOrderDeliveredPdfHandlerTests
{
    [Fact]
    public async Task Handle_generates_pdf_and_is_idempotent_on_same_MessageId()
    {
        var tenantContext = new AmbientTenantContext();
        await using var db = CreateDb(tenantContext);
        var time = TimeProvider.System;
        var tenant = Tenant.Create("Metalúrgica Demo", "1");
        typeof(Tenant).GetProperty(nameof(Tenant.RowVersion))!
            .SetValue(tenant, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var customer = Customer.Create(tenant.Id, "Cliente Acero Sur");
        var product = Product.Create(tenant.Id, "FG-BRACKET", "Soporte metálico", false);

        // Seed without ambient tenant (IgnoreQueryFilters not needed for inserts).
        db.Tenants.Add(tenant);
        db.Customers.Add(customer);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        tenantContext.SetTenant(tenant.Id);
        var wo = WorkOrder.Create(tenant, customer.Id, time);
        wo.AddLine(product.Id, 2);
        wo.StartProduction(time);
        wo.MarkDelivered(time);
        db.WorkOrders.Add(wo);
        var note = DeliveryNote.Create(tenant.Id, wo.Id, $"RN-{wo.Number}", time);
        note.AddLine(product.Id, 2);
        db.DeliveryNotes.Add(note);
        await db.SaveChangesAsync();
        tenantContext.Clear();

        var store = new InMemoryDeliveryNotePdfStore();
        var handler = new WorkOrderDeliveredPdfHandler(
            db,
            tenantContext,
            new QuestDeliveryNotePdfGenerator(),
            store,
            time,
            NullLogger<WorkOrderDeliveredPdfHandler>.Instance);

        var domainEvent = new WorkOrderDelivered(
            wo.Id,
            tenant.Id,
            wo.Number,
            note.Number,
            time.GetUtcNow());

        const string messageId = "msg-idempotent-1";
        await handler.HandleAsync(domainEvent, messageId);
        await handler.HandleAsync(domainEvent, messageId);

        (await store.ExistsAsync(tenant.Id, wo.Id)).Should().BeTrue();
        (await db.ProcessedMessages.CountAsync(m => m.MessageId == messageId)).Should().Be(1);

        await using var stream = await store.OpenReadAsync(tenant.Id, wo.Id);
        stream.Should().NotBeNull();
        using var ms = new MemoryStream();
        await stream!.CopyToAsync(ms);
        var text = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        text.Should().Contain(note.Number);
    }

    private static PymenDbContext CreateDb(AmbientTenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<PymenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PymenDbContext(options, tenantContext);
    }
}
