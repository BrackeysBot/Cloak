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
        var choices = new List<DiscordAutoCompleteChoice>();

        string? query = context.OptionValue?.ToString()?.Trim();
        bool isQueryEmpty = string.IsNullOrWhiteSpace(query);

        foreach (string group in groups)
        {
            if (isQueryEmpty || group.Contains(query!, StringComparison.OrdinalIgnoreCase))
            {
                choices.Add(new DiscordAutoCompleteChoice(group, group));
            }
        }

        return Task.FromResult(choices.AsEnumerable());
    }
}
