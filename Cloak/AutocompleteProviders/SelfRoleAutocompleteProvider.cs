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
        IReadOnlyList<DiscordRole> roles = roleService.GetDiscordRoles(context.Guild);
        var choices = new List<DiscordAutoCompleteChoice>();

        string? query = context.OptionValue?.ToString()?.Trim();
        bool isQueryEmpty = string.IsNullOrWhiteSpace(query);

        foreach (DiscordRole role in roles)
        {
            if (isQueryEmpty || role.Name.Contains(query!, StringComparison.OrdinalIgnoreCase))
            {
                choices.Add(new DiscordAutoCompleteChoice(role.Name, role.Id.ToString()));
            }
        }

        return Task.FromResult(choices.AsEnumerable());
    }
}
