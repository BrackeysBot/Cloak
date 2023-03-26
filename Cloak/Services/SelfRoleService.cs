using System.Diagnostics.CodeAnalysis;
using Cloak.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cloak.Services;

/// <summary>
///     Represents a service which manages self roles.
/// </summary>
internal sealed class SelfRoleService : BackgroundService
{
    private readonly ILogger<SelfRoleService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly Dictionary<DiscordGuild, Dictionary<SelfRole, DiscordRole>> _selfRoles = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelfRoleService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scopeFactory">The service scope factory.</param>
    /// <param name="discordClient">The Discord client.</param>
    public SelfRoleService(ILogger<SelfRoleService> logger, IServiceScopeFactory scopeFactory, DiscordClient discordClient)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Adds a new self-role to the database.
    /// </summary>
    /// <param name="guild">The guild in which the role is being created.</param>
    /// <param name="role">The Discord role to which the self role maps.</param>
    /// <param name="group">The group in which the role will live, or <see langword="null" /> to ignore group.</param>
    /// <returns>The newly-created <see cref="SelfRole" />.</returns>
    public async Task<SelfRole> AddSelfRoleAsync(DiscordGuild guild, DiscordRole role, string? group = null)
    {
        if (string.IsNullOrWhiteSpace(group)) group = null;

        var selfRole = new SelfRole {GuildId = guild.Id, RoleId = role.Id, Group = group};

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        EntityEntry<SelfRole> entry = await context.AddAsync(selfRole).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
        selfRole = entry.Entity;

        if (!_selfRoles.TryGetValue(guild, out Dictionary<SelfRole, DiscordRole>? selfRoles))
        {
            selfRoles = new Dictionary<SelfRole, DiscordRole>();
            _selfRoles[guild] = selfRoles;
        }

        selfRoles[selfRole] = role;

        _logger.LogInformation("Added self role {Role} to group {Group}", role, group ?? "<none>");
        return selfRole;
    }

    /// <summary>
    ///     Edits a self-role.
    /// </summary>
    /// <param name="guild">The guild in which the role is being created.</param>
    /// <param name="role">The Discord role to which the self role maps.</param>
    /// <param name="group">The new group in which the role will live, or <see langword="null" /> to clear the group.</param>
    public async Task<SelfRole?> EditSelfRoleAsync(DiscordGuild guild, DiscordRole role, string? group = null)
    {
        if (!TryGetSelfRole(guild, role, out SelfRole? selfRole)) return null;
        if (string.IsNullOrWhiteSpace(group)) group = null;

        selfRole.Group = group;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        context.Update(selfRole);
        await context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Edited self role {Role} with new group {Group}", role, group ?? "<none>");
        return selfRole;
    }

    /// <summary>
    ///     Gets the Discord roles associated with a self role defined for a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose self role-assigned roles to return.</param>
    /// <returns>A read-only view of the <see cref="DiscordRole" /> values defined for <paramref name="guild" />.</returns>
    public IReadOnlyList<DiscordRole> GetDiscordRoles(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _selfRoles.ContainsKey(guild) ? _selfRoles[guild].Values.ToArray() : ArraySegment<DiscordRole>.Empty;
    }

    /// <summary>
    ///     Gets the group to which a self-role has been assigned.
    /// </summary>
    /// <param name="guild">The guild whose roles to search.</param>
    /// <param name="role">The role whose group to return.</param>
    /// <returns>
    ///     The name of the group to which <paramref name="role" /> is assigned, if one is assigned; otherwise,
    ///     <see langword="null" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="role" /> is <see langword="null" />.
    /// </exception>
    public string? GetRoleGroup(DiscordGuild guild, DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(role);

        if (!_selfRoles.TryGetValue(guild, out Dictionary<SelfRole, DiscordRole>? selfRoles))
            return null;

        foreach ((SelfRole key, DiscordRole value) in selfRoles)
        {
            if (value == role)
                return key.Group;
        }

        return null;
    }

    /// <summary>
    ///     Returns all the roles which are assigned to a specified group.
    /// </summary>
    /// <param name="guild">The guild whose roles to search.</param>
    /// <param name="group">The group whose roles to return.</param>
    /// <returns>A read-only view of the roles that exist in <paramref name="group" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="group" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="group" /> is empty or consists of only whitespace.</exception>
    public IReadOnlyList<DiscordRole> GetRolesInGroup(DiscordGuild guild, string group)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(group);

        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Group cannot be empty.", nameof(group));

        if (!_selfRoles.TryGetValue(guild, out Dictionary<SelfRole, DiscordRole>? selfRoles))
            return ArraySegment<DiscordRole>.Empty;

        var roles = new List<DiscordRole>();

        foreach ((SelfRole key, DiscordRole value) in selfRoles)
        {
            if (string.Equals(key.Group, group, StringComparison.Ordinal))
                roles.Add(value);
        }

        return roles.AsReadOnly();
    }

    /// <summary>
    ///     Returns a read-only view of the self-role groups defined for a guild.
    /// </summary>
    /// <param name="guild">The guild whose self-role groups to return.</param>
    /// <returns>A <see cref="IReadOnlyList{T}" /> of <see cref="string" /> containing the group names.</returns>
    public IReadOnlyList<string> GetGroups(DiscordGuild guild)
    {
        if (!_selfRoles.TryGetValue(guild, out Dictionary<SelfRole, DiscordRole>? selfRoles))
            return ArraySegment<string>.Empty;

        var groups = new HashSet<string>();

        foreach (SelfRole roles in selfRoles.Keys)
        {
            if (roles.Group is null) continue;
            groups.Add(roles.Group);
        }

        return groups.OrderBy(g => g).ToArray();
    }

    /// <summary>
    ///     Returns a value indicating whether the specified role is considered a self role.
    /// </summary>
    /// <param name="guild">The guild in which the check should be performed.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="role" /> is a self role; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="role" /> is <see langword="null" />.
    /// </exception>
    public bool IsSelfRole(DiscordGuild guild, DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(role);

        return _selfRoles.TryGetValue(guild, out Dictionary<SelfRole, DiscordRole>? selfRoles) && selfRoles.ContainsValue(role);
    }

    /// <summary>
    ///     Removes a self-role.
    /// </summary>
    /// <param name="guild">The guild in which the role is being removed.</param>
    /// <param name="role">The Discord to no longer consider a self role.</param>
    public async Task RemoveSelfRoleAsync(DiscordGuild guild, DiscordRole role)
    {
        if (!TryGetSelfRole(guild, role, out SelfRole? selfRole)) return;

        _selfRoles[guild].Remove(selfRole);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        context.Remove(selfRole);
        await context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Removed self role {Role}", role);
    }

    /// <summary>
    ///     Returns a value indicating whether the specified role is a valid self-role.
    /// </summary>
    /// <param name="guild">The guild whose roles to search.</param>
    /// <param name="role">The role to check.</param>
    /// <param name="selfRole">
    ///     When this method returns, contains the self role which owns <paramref name="role" />, if a role was found; otherwise,
    ///     <see langword="null" />.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="role" /> corresponds to a valid self role; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="role" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetSelfRole(DiscordGuild guild, DiscordRole role, [NotNullWhen(true)] out SelfRole? selfRole)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(role);

        if (!_selfRoles.TryGetValue(guild, out Dictionary<SelfRole, DiscordRole>? selfRoles))
        {
            selfRole = null;
            return false;
        }

        foreach ((SelfRole key, DiscordRole value) in selfRoles)
        {
            if (value == role)
            {
                selfRole = key;
                return true;
            }
        }

        selfRole = null;
        return false;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);

        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
    }

    private Dictionary<SelfRole, DiscordRole> AddOrGetSelfRoles(DiscordGuild guild)
    {
        if (_selfRoles.TryGetValue(guild, out Dictionary<SelfRole, DiscordRole>? selfRoles))
        {
            selfRoles.Clear();
        }
        else
        {
            selfRoles = new Dictionary<SelfRole, DiscordRole>();
            _selfRoles[guild] = selfRoles;
        }

        return selfRoles;
    }

    private async Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();

        var rolesToRemove = new List<SelfRole>();

        Dictionary<SelfRole, DiscordRole> selfRoles = AddOrGetSelfRoles(e.Guild);

        foreach (SelfRole selfRole in context.SelfRoles.Where(r => r.GuildId == e.Guild.Id))
        {
            DiscordRole? role = e.Guild.GetRole(selfRole.RoleId);
            if (role is null)
                rolesToRemove.Add(selfRole);
            else
                selfRoles[selfRole] = role;
        }

        context.RemoveRange(rolesToRemove);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
