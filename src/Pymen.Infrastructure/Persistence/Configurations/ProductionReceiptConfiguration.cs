using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pymen.Domain.Catalog;
using Pymen.Domain.Inventory;
using Pymen.Domain.Production;
using Pymen.Domain.Tenancy;

namespace Pymen.Infrastructure.Persistence.Configurations;

public sealed class ProductionReceiptConfiguration : IEntityTypeConfiguration<ProductionReceipt>
{
    public void Configure(EntityTypeBuilder<ProductionReceipt> builder)
    {
        builder.ToTable("ProductionReceipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkOrder>()
            .WithMany()
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
