using Cloak.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cloak.Services;

internal sealed class MemberRoleService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly PersistentRoleService _persistentRoleService;
    private readonly SelfRoleService _selfRoleService;
    private readonly Dictionary<DiscordGuild, List<MemberRoles>> _memberRoles = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemberRoleService" /> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="persistentRoleService">The persistent role service.</param>
    /// <param name="selfRoleService">The self role service.</param>
    public MemberRoleService(
        IServiceScopeFactory scopeFactory,
        DiscordClient discordClient,
        PersistentRoleService persistentRoleService,
        SelfRoleService selfRoleService
    )
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _persistentRoleService = persistentRoleService;
        _selfRoleService = selfRoleService;
    }

    /// <summary>
    ///     Applies the member's persistent roles.
    /// </summary>
    /// <param name="member">The member whose roles to apply.</param>
    /// <returns>The number of roles applied to <paramref name="member" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public async Task<int> ApplyPersistentRolesAsync(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var tasks = new List<Task>();

        foreach (DiscordRole discordRole in GetPersistentRoles(member.Guild, member))
        {
            tasks.Add(member.GrantRoleAsync(discordRole));
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks).ConfigureAwait(false);

        return tasks.Count;
    }

    /// <summary>
    ///     Applies the member's persistent roles.
    /// </summary>
    /// <param name="member">The member whose roles to apply.</param>
    /// <param name="discordRole">The role to add.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="member" /> or <paramref name="discordRole" /> is <see langword="null" />.
    /// </exception>
    public async Task<bool> ApplySelfRoleAsync(DiscordMember member, DiscordRole discordRole)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(discordRole);

        DiscordGuild guild = member.Guild;

        if (!_selfRoleService.IsSelfRole(guild, discordRole))
            return false;

        string? group = _selfRoleService.GetRoleGroup(guild, discordRole);
        await member.GrantRoleAsync(discordRole).ConfigureAwait(false);

        var tasks = new List<Task>();
        if (!string.IsNullOrWhiteSpace(group))
        {
            foreach (DiscordRole role in _selfRoleService.GetRolesInGroup(guild, group))
            {
                if (role != discordRole)
                    tasks.Add(member.RevokeRoleAsync(role));
            }
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    ///     Gets the persistent roles that the specified user had when leaving the specified guild.
    /// </summary>
    /// <param name="guild">The guild to search.</param>
    /// <param name="user">The user whose persistent roles to return.</param>
    /// <returns>A read-only view of the roles that this user should reattain.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="user" /> is <see langword="null" />.
    /// </exception>
    public IReadOnlyList<DiscordRole> GetPersistentRoles(DiscordGuild guild, DiscordUser user)
    {
        var roles = new List<DiscordRole>();

        foreach (ulong roleId in GetMemberRoles(guild, user).SelectMany(r => r.RoleIds))
        {
            if (guild.Roles.TryGetValue(roleId, out DiscordRole? discordRole) &&
                _persistentRoleService.IsPersistentRole(guild, discordRole))
            {
                roles.Add(discordRole);
            }
        }

        return roles.Count == 0 ? ArraySegment<DiscordRole>.Empty : roles.AsReadOnly();
    }

    /// <summary>
    ///     Removes a member role cache entry from the database.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="user">The user whose roles to clear from the database.</param>
    public async Task RemoveFromDatabaseAsync(DiscordGuild guild, DiscordUser user)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();

        var memberRolesToRemove = new List<MemberRoles>();
        await foreach (MemberRoles memberRole in context.MemberRoles)
        {
            if (memberRole.GuildId == guild.Id && memberRole.UserId == user.Id)
                memberRolesToRemove.Add(memberRole);
        }

        if (memberRolesToRemove.Count > 0)
        {
            context.RemoveRange(memberRolesToRemove);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Saves the persistent roles for a member.
    /// </summary>
    /// <param name="member">The member whose roles to save.</param>
    public async Task<int> SavePersistentRolesAsync(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        DiscordGuild guild = member.Guild;
        IEnumerable<DiscordRole> discordRoles = member.Roles.Where(r => _persistentRoleService.IsPersistentRole(guild, r));
        ulong[] roleIds = discordRoles.Select(r => r.Id).ToArray();
        if (roleIds.Length == 0)
            return 0;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();

        EntityEntry<MemberRoles> entry = await context.AddAsync(new MemberRoles
        {
            GuildId = guild.Id,
            UserId = member.Id,
            RoleIds = roleIds
        }).ConfigureAwait(false);

        List<MemberRoles> memberRoles = AddOrGetMemberRoles(member.Guild);
        memberRoles.Add(entry.Entity);

        await context.SaveChangesAsync().ConfigureAwait(false);
        return roleIds.Length;
    }

    /// <summary>
    ///     Updates the member role cache from the database.
    /// </summary>
    /// <param name="guild">The guild whose member roles to update.</param>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public async Task UpdateRoleCacheAsync(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (_memberRoles.TryGetValue(guild, out List<MemberRoles>? memberRoles))
        {
            memberRoles.Clear();
        }
        else
        {
            memberRoles = new List<MemberRoles>();
            _memberRoles[guild] = memberRoles;
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();

        await foreach (MemberRoles memberRole in context.MemberRoles)
        {
            memberRoles.Add(memberRole);
        }
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        return Task.CompletedTask;
    }

    private List<MemberRoles> AddOrGetMemberRoles(DiscordGuild guild)
    {
        if (_memberRoles.TryGetValue(guild, out List<MemberRoles>? memberRoles))
        {
            memberRoles.Clear();
        }
        else
        {
            memberRoles = new List<MemberRoles>();
            _memberRoles[guild] = memberRoles;
        }

        return memberRoles;
    }

    private IReadOnlyList<MemberRoles> GetMemberRoles(DiscordGuild guild, DiscordUser user)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(user);

        if (!_memberRoles.TryGetValue(guild, out List<MemberRoles>? memberRoles))
            return ArraySegment<MemberRoles>.Empty;

        if (memberRoles.Count == 0)
            return ArraySegment<MemberRoles>.Empty;

        var roles = new List<MemberRoles>();

        foreach (MemberRoles memberRole in memberRoles)
        {
            if (memberRole.GuildId == guild.Id && memberRole.UserId == user.Id)
                roles.Add(memberRole);
        }

        return roles.Count == 0 ? ArraySegment<MemberRoles>.Empty : roles.AsReadOnly();
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        return UpdateRoleCacheAsync(e.Guild);
    }
}
