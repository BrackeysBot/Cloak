using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cloak.Data.EntityConfigurations;

internal sealed class InformationEmbedSectionConfiguration : IEntityTypeConfiguration<InformationEmbedSection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InformationEmbedSection> builder)
    {
        builder.ToTable(nameof(InformationEmbedSection));
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasConversion<GuidToBytesConverter>();
        builder.Property(e => e.GuildId);
        builder.Property(e => e.Order);
        builder.Property(e => e.Title).HasMaxLength(256);
        builder.Property(e => e.Body).HasMaxLength(1024);
    }
}
