using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cloak.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="SelfRole" />.
/// </summary>
internal sealed class SelfRoleConfiguration : IEntityTypeConfiguration<SelfRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SelfRole> builder)
    {
        builder.ToTable(nameof(SelfRole));
        builder.HasKey(e => new {e.GuildId, e.RoleId});

        builder.Property(e => e.GuildId);
        builder.Property(e => e.RoleId);
        builder.Property(e => e.Group);
    }
}
