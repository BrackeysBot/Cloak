using Cloak.AutocompleteProviders;
using Cloak.Data;
using Cloak.Services;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Cloak.Commands;

/// <summary>
///     Represents a class which implements the <c>selfrole</c> command.
/// </summary>
[SlashCommandGroup("selfrole", "Manages self roles", false)]
internal sealed class SelfRoleCommand : ApplicationCommandModule
{
    private readonly SelfRoleService _selfRoleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelfRoleCommand" /> class.
    /// </summary>
    /// <param name="selfRoleService">The role service.</param>
    public SelfRoleCommand(SelfRoleService selfRoleService)
    {
        _selfRoleService = selfRoleService;
    }

    [SlashCommand("add", "Adds a new self-role", false)]
    [SlashRequireGuild]
    public async Task AddAsync(
        InteractionContext context,
        [Option("role", "The role to add.")] DiscordRole role,
        [Option("group", "The group to assign.", true), Autocomplete(typeof(GroupAutocompleteProvider))] string? group = null
    )
    {
        var embed = new DiscordEmbedBuilder();
        if (_selfRoleService.IsSelfRole(context.Guild, role))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Error");
            embed.WithDescription($"The {role.Mention} role is already assigned as a self-role.");
            await context.CreateResponseAsync(embed).ConfigureAwait(false);
            return;
        }

        SelfRole selfRole = await _selfRoleService.AddSelfRoleAsync(context.Guild, role, group).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Self-role added");
        embed.AddField("Role", role.Mention, true);
        embed.AddField("Group", selfRole.Group ?? "<none>", true);
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }

    [SlashCommand("edit", "Edits a self-role", false)]
    [SlashRequireGuild]
    public async Task EditAsync(
        InteractionContext context,
        [Option("role", "The role to edit.")] DiscordRole role,
        [Option("group", "The new group to assign. Leave unspecified to clear the group.", true)]
        [Autocomplete(typeof(GroupAutocompleteProvider))]
        string? group = null
    )
    {
        var embed = new DiscordEmbedBuilder();
        if (!_selfRoleService.IsSelfRole(context.Guild, role))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Error");
            embed.WithDescription($"The {role.Mention} role is not a self-role.");
            await context.CreateResponseAsync(embed).ConfigureAwait(false);
            return;
        }

        SelfRole? selfRole = await _selfRoleService.EditSelfRoleAsync(context.Guild, role, group).ConfigureAwait(false);
        if (selfRole is null)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Unreachable error");
            embed.WithDescription($"The {role.Mention} role is not a self-role.");
            embed.WithFooter("Uh oh! You should not be able to see this error!");
            await context.CreateResponseAsync(embed).ConfigureAwait(false);
            return;
        }

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Self-role edited");
        embed.AddField("Role", role.Mention, true);
        embed.AddField("New Group", selfRole.Group ?? "<none>", true);
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }

    [SlashCommand("remove", "Removes a self-role", false)]
    [SlashRequireGuild]
    public async Task RemoveAsync(InteractionContext context, [Option("role", "The role to remove.")] DiscordRole role)
    {
        var embed = new DiscordEmbedBuilder();
        if (!_selfRoleService.IsSelfRole(context.Guild, role))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Error");
            embed.WithDescription($"The {role.Mention} role is not a self-role.");
            await context.CreateResponseAsync(embed).ConfigureAwait(false);
            return;
        }

        await _selfRoleService.RemoveSelfRoleAsync(context.Guild, role).ConfigureAwait(false);
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Self-role removed");
        embed.WithDescription($"The role {role.Mention} has been removed from the self-roles database.");
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
