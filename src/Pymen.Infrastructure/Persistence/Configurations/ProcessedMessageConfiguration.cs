using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pymen.Infrastructure.Messaging;

namespace Pymen.Infrastructure.Persistence.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("ProcessedMessages");
        builder.HasKey(x => x.MessageId);
        builder.Property(x => x.MessageId).HasMaxLength(64);
        builder.Property(x => x.EventType).HasMaxLength(256).IsRequired();
    }
}
