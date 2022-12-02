using Cloak.Services;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

namespace Cloak.AutocompleteProviders;

/// <summary>
///     Provides autocomplete suggestions for information embed sections.
/// </summary>
internal sealed class InformationEmbedSectionAutocompleteProvider : IAutocompleteProvider
{
    // this is quite possibly the most Java-esque class name I've ever written in C# and I have very strong feelings about it.

    /// <inheritdoc />
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        var informationEmbedService = context.Services.GetRequiredService<InformationEmbedService>();
        return Task.FromResult(informationEmbedService.GetInformationEmbedSections(context.Guild, false)
            .Select(s => new DiscordAutoCompleteChoice(s.Title, s.Id.ToString())));
    }
}
