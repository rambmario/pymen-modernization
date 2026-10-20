using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pymen.Domain.Catalog;
using Pymen.Domain.Inventory;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Infrastructure.Persistence.Seed;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");
        var tenantContext = scope.ServiceProvider.GetRequiredService<AmbientTenantContext>();

        await db.Database.MigrateAsync(cancellationToken);

        // System process: no ambient tenant; use IgnoreQueryFilters for any tenant-owned reads.
        tenantContext.Clear();

        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            logger.LogInformation("Demo data already present; skipping seed.");
            return;
        }

        var metal = Tenant.Create("Metalúrgica Demo", "1");
        var textile = Tenant.Create("Textil Demo", "2");
        db.Tenants.AddRange(metal, textile);
        await db.SaveChangesAsync(cancellationToken);

        await SeedTenantAsync(db, time, metal, isMetal: true, cancellationToken);
        await SeedTenantAsync(db, time, textile, isMetal: false, cancellationToken);

        logger.LogInformation("Seeded demo tenants '{Metal}' and '{Textile}'.", metal.Name, textile.Name);
    }

    private static async Task SeedTenantAsync(
        PymenDbContext db,
        TimeProvider time,
        Tenant tenant,
        bool isMetal,
        CancellationToken cancellationToken)
    {
        var customerA = Customer.Create(tenant.Id, isMetal ? "Cliente Acero Sur" : "Cliente Telas Norte");
        var customerB = Customer.Create(tenant.Id, isMetal ? "Cliente Tornillos SA" : "Cliente Confecciones Este");

        var finished = Product.Create(
            tenant.Id,
            isMetal ? "FG-BRACKET" : "FG-SHIRT",
            isMetal ? "Soporte metálico" : "Camisa básica",
            isRawMaterial: false);

        var raw = Product.Create(
            tenant.Id,
            isMetal ? "RM-STEEL" : "RM-COTTON",
            isMetal ? "Plancha de acero" : "Tela de algodón",
            isRawMaterial: true);

        var bom = BillOfMaterialsLine.Create(tenant.Id, finished.Id, raw.Id, isMetal ? 1.5m : 2.0m);
        var warehouse = Warehouse.Create(tenant.Id, "MAIN", "Depósito principal");
        var rawStock = StockBalance.Create(tenant.Id, raw.Id, warehouse.Id, 500);
        var finishedStock = StockBalance.Create(tenant.Id, finished.Id, warehouse.Id, 0);
        var cutting = OperationType.Create(tenant.Id, "CUT", isMetal ? "Corte" : "Corte de tela");
        var weldOrSew = OperationType.Create(tenant.Id, isMetal ? "WELD" : "SEW", isMetal ? "Soldadura" : "Costura");

        db.AddRange(customerA, customerB, finished, raw, bom, warehouse, rawStock, finishedStock, cutting, weldOrSew);
        await db.SaveChangesAsync(cancellationToken);

        var draft = WorkOrder.Create(tenant, customerA.Id, time, "OT demo borrador");
        draft.AddLine(finished.Id, 10);
        draft.AddOperation(cutting.Id);

        var inProduction = WorkOrder.Create(tenant, customerB.Id, time, "OT demo en producción");
        inProduction.AddLine(finished.Id, 5);
        inProduction.AddOperation(cutting.Id);
        inProduction.AddOperation(weldOrSew.Id);
        inProduction.StartProduction(time);

        var delivered = WorkOrder.Create(tenant, customerA.Id, time, "OT demo entregada");
        delivered.AddLine(finished.Id, 2);
        delivered.StartProduction(time);
        delivered.MarkDelivered(time);

        db.WorkOrders.AddRange(draft, inProduction, delivered);
        await db.SaveChangesAsync(cancellationToken);
    }
}
