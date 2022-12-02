using Cloak.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Cloak.Services;

/// <summary>
///     Represents a service which listens for member events, so that their persistent roles can be cached.
/// </summary>
internal sealed class MemberWatchdogService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly MemberRoleService _memberRoleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemberWatchdogService" /> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="memberRoleService">The member role service.</param>
    public MemberWatchdogService(
        IServiceScopeFactory scopeFactory,
        DiscordClient discordClient,
        MemberRoleService memberRoleService
    )
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _memberRoleService = memberRoleService;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);

        _discordClient.GuildMemberAdded += DiscordClientOnGuildMemberAdded;
        _discordClient.GuildMemberRemoved += DiscordClientOnGuildMemberRemoved;
    }

    private async Task DiscordClientOnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        DiscordMember member = e.Member;
        try
        {
            int count = await _memberRoleService.ApplyPersistentRolesAsync(member).ConfigureAwait(false);
            if (count > 0)
                Logger.Info($"Restored {"persistent role".ToQuantity(count)} for {member}");

            await _memberRoleService.RemoveFromDatabaseAsync(member.Guild, member).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not restore persistent roles for {member}");
        }
    }

    private async Task DiscordClientOnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
    {
        DiscordMember member = e.Member;
        try
        {
            int count = await _memberRoleService.SavePersistentRolesAsync(member).ConfigureAwait(false);
            if (count > 0)
                Logger.Info($"Saved {"persistent role".ToQuantity(count)} for {member}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not save persistent roles for {member}");
        }
    }
}
