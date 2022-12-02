using Cloak.Services;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

namespace Cloak.AutocompleteProviders;

/// <summary>
///     Provides autocomplete suggestions for self-role groups.
/// </summary>
internal sealed class GroupAutocompleteProvider : IAutocompleteProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        var roleService = context.Services.GetRequiredService<SelfRoleService>();
        IReadOnlyList<string> groups = roleService.GetGroups(context.Guild);
        return Task.FromResult(groups.Select(g => new DiscordAutoCompleteChoice(g, g)));
    }
}
