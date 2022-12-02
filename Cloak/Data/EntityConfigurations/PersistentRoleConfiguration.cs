using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cloak.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="PersistentRole" />.
/// </summary>
internal sealed class PersistentRoleConfiguration : IEntityTypeConfiguration<PersistentRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PersistentRole> builder)
    {
        builder.ToTable(nameof(PersistentRole));
        builder.HasKey(e => new {e.GuildId, e.RoleId});

        builder.Property(e => e.GuildId);
        builder.Property(e => e.RoleId);
    }
}
