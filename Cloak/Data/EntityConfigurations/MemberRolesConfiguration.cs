using Cloak.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cloak.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="MemberRoles" />.
/// </summary>
internal sealed class MemberRolesConfiguration : IEntityTypeConfiguration<MemberRoles>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MemberRoles> builder)
    {
        builder.ToTable(nameof(MemberRoles));
        builder.HasKey(e => new {e.GuildId, e.UserId});

        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);
        builder.Property(e => e.RoleIds).HasConversion<UInt64ListToBytesConverter>();
    }
}
