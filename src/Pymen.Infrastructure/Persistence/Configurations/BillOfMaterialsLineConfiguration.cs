using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pymen.Domain.Catalog;
using Pymen.Domain.Tenancy;

namespace Pymen.Infrastructure.Persistence.Configurations;

public sealed class BillOfMaterialsLineConfiguration : IEntityTypeConfiguration<BillOfMaterialsLine>
{
    public void Configure(EntityTypeBuilder<BillOfMaterialsLine> builder)
    {
        builder.ToTable("BillOfMaterialsLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityPerUnit).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.TenantId, x.FinishedProductId, x.RawMaterialProductId }).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.FinishedProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.RawMaterialProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
