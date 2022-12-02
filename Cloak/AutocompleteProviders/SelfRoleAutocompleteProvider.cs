using Cloak.Services;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

namespace Cloak.AutocompleteProviders;

/// <summary>
///     Provides autocomplete suggestions for self-roles.
/// </summary>
internal sealed class SelfRoleAutocompleteProvider : IAutocompleteProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        var roleService = context.Services.GetRequiredService<SelfRoleService>();
        return Task.FromResult(roleService.GetDiscordRoles(context.Guild)
            .Select(role => new DiscordAutoCompleteChoice(role.Name, role.Id.ToString())));
    }
}
