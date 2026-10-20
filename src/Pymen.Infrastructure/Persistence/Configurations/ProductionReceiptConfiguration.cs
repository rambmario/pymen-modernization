using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pymen.Domain.Production;

namespace Pymen.Infrastructure.Persistence.Configurations;

public sealed class ProductionReceiptConfiguration : IEntityTypeConfiguration<ProductionReceipt>
{
    public void Configure(EntityTypeBuilder<ProductionReceipt> builder)
    {
        builder.ToTable("ProductionReceipts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
    }
}
