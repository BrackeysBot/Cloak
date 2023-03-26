using Cloak.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cloak.Services;

/// <summary>
///     Represents a service which manages persistent roles.
/// </summary>
internal sealed class PersistentRoleService : BackgroundService
{
    private readonly ILogger<PersistentRoleService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly Dictionary<DiscordGuild, HashSet<DiscordRole>> _persistentRoles = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="PersistentRoleService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="discordClient">The Discord client.</param>
    public PersistentRoleService(ILogger<PersistentRoleService> logger, IServiceScopeFactory scopeFactory, DiscordClient discordClient)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Adds a persistent role.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="role">The role to mark as persistent.</param>
    /// <returns>
    ///     <see langword="true" /> if the persistent role was successfully added; otherwise, <see langword="false" />.
    /// </returns>
    public async Task<bool> AddPersistentRoleAsync(DiscordGuild guild, DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(role);

        if (_persistentRoles.TryGetValue(guild, out HashSet<DiscordRole>? persistentRoles))
        {
            if (persistentRoles.Contains(role))
            {
                return false;
            }
        }
        else
        {
            persistentRoles = new HashSet<DiscordRole>();
            _persistentRoles[guild] = persistentRoles;
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        if (await context.PersistentRoles.AnyAsync(r => r.GuildId == guild.Id && r.RoleId == role.Id).ConfigureAwait(false))
            return false;

        var persistentRole = new PersistentRole {GuildId = guild.Id, RoleId = role.Id};
        await context.AddAsync(persistentRole).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return persistentRoles.Add(role);
    }

    /// <summary>
    ///     Gets a read-only view of the persistent roles for a guild.
    /// </summary>
    /// <param name="guild">The guild whose persistent roles to return.</param>
    /// <returns>A read-only collection of <see cref="DiscordRole" /> objects.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyCollection<DiscordRole> GetPersistentRoles(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        return _persistentRoles.TryGetValue(guild, out HashSet<DiscordRole>? hashSet)
            ? hashSet.ToArray()
            : ArraySegment<DiscordRole>.Empty;
    }

    /// <summary>
    ///     Returns a value indicating whether the specified role is considered a persistent role.
    /// </summary>
    /// <param name="guild">The guild in which the persistence check should be performed.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="role" /> is a persistent role; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="role" /> is <see langword="null" />.
    /// </exception>
    public bool IsPersistentRole(DiscordGuild guild, DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(role);

        return _persistentRoles.TryGetValue(guild, out HashSet<DiscordRole>? persistentRoles) && persistentRoles.Contains(role);
    }

    /// <summary>
    ///     Removes a role from being persistent.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="role">The role to no longer mark as persistent.</param>
    /// <returns>
    ///     <see langword="true" /> if the persistent role was successfully removed; otherwise, <see langword="false" />.
    /// </returns>
    public async Task<bool> RemovePersistentRoleAsync(DiscordGuild guild, DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(role);

        if (!_persistentRoles.TryGetValue(guild, out HashSet<DiscordRole>? persistentRoles))
        {
            return false;
        }

        if (!persistentRoles.Contains(role))
        {
            return false;
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();

        PersistentRole? persistentRole = await context.PersistentRoles
            .FirstOrDefaultAsync(r => r.GuildId == guild.Id && r.RoleId == role.Id)
            .ConfigureAwait(false);

        if (persistentRole is null)
        {
            return false;
        }

        context.Remove(persistentRole);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return persistentRoles.Remove(role);
    }

    /// <summary>
    ///     Removes stale roles from the database; that is, roles that no longer map to existing roles in the guild.
    /// </summary>
    /// <param name="guild">The guild whose stale roles to prune.</param>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public async Task RemoveStaleRolesAsync(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();

        var rolesToRemove = new List<PersistentRole>();

        foreach (PersistentRole persistentRole in context.PersistentRoles.Where(r => r.GuildId == guild.Id))
        {
            if (!guild.Roles.TryGetValue(persistentRole.RoleId, out DiscordRole? _))
                rolesToRemove.Add(persistentRole);
        }

        if (rolesToRemove.Count > 0)
        {
            context.RemoveRange(rolesToRemove);
            await context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Removed {Quantity} from the database that mapped to invalid roles", "roles".ToQuantity(rolesToRemove.Count));
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);

        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
    }

    private HashSet<DiscordRole> AddOrGetPersistentRoles(DiscordGuild guild)
    {
        if (_persistentRoles.TryGetValue(guild, out HashSet<DiscordRole>? persistentRoles))
        {
            persistentRoles.Clear();
        }
        else
        {
            persistentRoles = new HashSet<DiscordRole>();
            _persistentRoles[guild] = persistentRoles;
        }

        return persistentRoles;
    }

    private async Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();

        DiscordGuild guild = e.Guild;
        HashSet<DiscordRole> persistentRoles = AddOrGetPersistentRoles(guild);
        await RemoveStaleRolesAsync(guild).ConfigureAwait(false);

        var rolesToRemove = new List<PersistentRole>();
        foreach (PersistentRole persistentRole in context.PersistentRoles.Where(r => r.GuildId == guild.Id))
        {
            if (guild.Roles.TryGetValue(persistentRole.RoleId, out DiscordRole? discordRole))
                persistentRoles.Add(discordRole);
            else
                rolesToRemove.Add(persistentRole);
        }

        if (rolesToRemove.Count > 0)
        {
            context.RemoveRange(rolesToRemove);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
