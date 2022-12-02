using Cloak.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Cloak.Data;

/// <summary>
///     Represents a connection to the <c>cloak.db</c> database.
/// </summary>
internal sealed class CloakContext : DbContext
{
    /// <summary>
    ///     Gets the set of information embed sections.
    /// </summary>
    /// <value>The set of information embed sections.</value>
    public DbSet<InformationEmbedSection> InformationEmbedSections { get; set; } = null!;

    /// <summary>
    ///     Gets the set of member roles.
    /// </summary>
    /// <value>The set of member roles.</value>
    public DbSet<MemberRoles> MemberRoles { get; internal set; } = null!;

    /// <summary>
    ///     Gets the set of persistent roles.
    /// </summary>
    /// <value>The set of persistent roles.</value>
    public DbSet<PersistentRole> PersistentRoles { get; internal set; } = null!;

    /// <summary>
    ///     Gets the set of self roles.
    /// </summary>
    /// <value>The set of self roles.</value>
    public DbSet<SelfRole> SelfRoles { get; internal set; } = null!;

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseSqlite("Data Source='data/cloak.db'");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new InformationEmbedSectionConfiguration());
        modelBuilder.ApplyConfiguration(new MemberRolesConfiguration());
        modelBuilder.ApplyConfiguration(new PersistentRoleConfiguration());
        modelBuilder.ApplyConfiguration(new SelfRoleConfiguration());
    }
}
