using Cloak.AutocompleteProviders;
using Cloak.Services;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using X10D.DSharpPlus;

namespace Cloak.Commands;

/// <summary>
///     Represents a class which implements the <c>role</c> command.
/// </summary>
[SlashCommandGroup("role", "Adds or removes self-roles.")]
internal sealed class RoleCommand : ApplicationCommandModule
{
    private readonly MemberRoleService _memberRoleService;
    private readonly SelfRoleService _selfRoleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RoleCommand" /> class.
    /// </summary>
    /// <param name="memberRoleService">The member role service.</param>
    /// <param name="selfRoleService">The self-role service.</param>
    public RoleCommand(MemberRoleService memberRoleService, SelfRoleService selfRoleService)
    {
        _memberRoleService = memberRoleService;
        _selfRoleService = selfRoleService;
    }

    [SlashCommand("give", "Grants a self-role.")]
    [SlashRequireGuild]
    public async Task GiveRoleAsync(
        InteractionContext context,
        [Option("role", "The role to gain.", true), Autocomplete(typeof(SelfRoleAutocompleteProvider))] string roleInput
    )
    {
        var embed = new DiscordEmbedBuilder();
        DiscordRole? role;
        DiscordGuild guild = context.Guild;

        if (ulong.TryParse(roleInput, out ulong roleId) || MentionUtility.TryParseRole(roleInput, out roleId))
            role = guild.GetRole(roleId);
        else
            role = guild.Roles.Values.FirstOrDefault(r => string.Equals(r.Name, roleInput, StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"No role matching `{roleInput}` was found.");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (_selfRoleService.IsSelfRole(guild, role))
        {
            await _memberRoleService.ApplySelfRoleAsync(context.Member, role).ConfigureAwait(false);
            embed.WithColor(DiscordColor.Green);
            embed.WithDescription($"You have successfully gained the {role.Mention} role.");
        }
        else
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"You do not have permission to gain the {role.Mention} role.");
        }

        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }

    [SlashCommand("remove", "Removes a self-role.")]
    [SlashRequireGuild]
    public async Task RemoveRoleAsync(
        InteractionContext context,
        [Option("role", "The role to remove.", true), Autocomplete(typeof(SelfRoleAutocompleteProvider))] string roleInput
    )
    {
        var embed = new DiscordEmbedBuilder();
        DiscordRole? role;
        DiscordGuild guild = context.Guild;

        if (ulong.TryParse(roleInput, out ulong roleId) || MentionUtility.TryParseRole(roleInput, out roleId))
            role = guild.GetRole(roleId);
        else
            role = guild.Roles.Values.FirstOrDefault(r => string.Equals(r.Name, roleInput, StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"No role matching `{roleInput}` was found.");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (_selfRoleService.IsSelfRole(context.Guild, role))
        {
            await context.Member.RevokeRoleAsync(role, "Self-role removed").ConfigureAwait(false);
            embed.WithColor(DiscordColor.Green);
            embed.WithDescription($"You have successfully removed the {role.Mention} role.");
        }
        else
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"You do not have permission to removed the {role.Mention} role.");
        }

        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
