using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pymen.Domain.Catalog;

namespace Pymen.Infrastructure.Persistence.Configurations;

public sealed class BillOfMaterialsLineConfiguration : IEntityTypeConfiguration<BillOfMaterialsLine>
{
    public void Configure(EntityTypeBuilder<BillOfMaterialsLine> builder)
    {
        builder.ToTable("BillOfMaterialsLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QuantityPerUnit).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.TenantId, x.FinishedProductId, x.RawMaterialProductId }).IsUnique();
    }
}
