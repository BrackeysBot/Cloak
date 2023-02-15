using System.Text;
using Cloak.Services;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Humanizer;

namespace Cloak.Commands;

/// <summary>
///     Represents a class which implements the <c>persistentrole</c> command.
/// </summary>
[SlashCommandGroup("persistentrole", "Manages persistent roles.", false)]
internal sealed class PersistentRoleCommand : ApplicationCommandModule
{
    private readonly PersistentRoleService _persistentRoleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PersistentRoleCommand" /> class.
    /// </summary>
    /// <param name="persistentRoleService">The persistent role service.</param>
    public PersistentRoleCommand(PersistentRoleService persistentRoleService)
    {
        _persistentRoleService = persistentRoleService;
    }

    [SlashCommand("add", "Adds a new persistent role", false)]
    [SlashRequireGuild]
    public async Task AddAsync(
        InteractionContext context,
        [Option("role", "The role to add.")] DiscordRole role
    )
    {
        var embed = new DiscordEmbedBuilder();
        bool result = await _persistentRoleService.AddPersistentRoleAsync(context.Guild, role).ConfigureAwait(false);

        if (result)
        {
            embed.WithColor(DiscordColor.Green);
            embed.WithDescription($"The {role.Mention} role has been marked persistent.");
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithDescription($"The {role.Mention} role is already persistent.");
        }

        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }

    [SlashCommand("list", "Lists the current persistent roles.", false)]
    [SlashRequireGuild]
    public async Task ListAsync(InteractionContext context)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("Persistent roles");

        IReadOnlyCollection<DiscordRole> roles = _persistentRoleService.GetPersistentRoles(context.Guild);
        if (roles.Count == 0)
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithDescription("No persistent roles have been set for this guild.");
        }
        else
        {
            embed.WithColor(DiscordColor.Green);
            var builder = new StringBuilder();

            foreach (DiscordRole role in roles)
            {
                builder.AppendLine($"- {role.Mention} ({role.Id})");
            }

            embed.WithDescription($"{"persistent role".ToQuantity(roles.Count)} currently defined:\n\n{builder}");
        }

        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }

    [SlashCommand("remove", "Removes a persistent role", false)]
    [SlashRequireGuild]
    public async Task RemoveAsync(
        InteractionContext context,
        [Option("role", "The role to add.")] DiscordRole role
    )
    {
        var embed = new DiscordEmbedBuilder();
        bool result = await _persistentRoleService.RemovePersistentRoleAsync(context.Guild, role).ConfigureAwait(false);

        if (result)
        {
            embed.WithColor(DiscordColor.Green);
            embed.WithDescription($"The {role.Mention} role has been un-marked as persistent.");
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithDescription($"The {role.Mention} role was already not persistent.");
        }

        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
