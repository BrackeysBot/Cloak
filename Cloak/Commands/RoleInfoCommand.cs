using System.Globalization;
using Cloak.AutocompleteProviders;
using Cloak.Data;
using Cloak.Interactivity;
using Cloak.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Cloak.Commands;

/// <summary>
///     Represents a class which implements the <c>roleinfo</c> command.
/// </summary>
[SlashCommandGroup("roleinfo", "Manages the role information embed.", false)]
internal sealed class RoleInfoCommand : ApplicationCommandModule
{
    private readonly InformationEmbedService _informationEmbedService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfoCommand" /> class.
    /// </summary>
    /// <param name="informationEmbedService">The information embed service.</param>
    public RoleInfoCommand(InformationEmbedService informationEmbedService)
    {
        _informationEmbedService = informationEmbedService;
    }

    [SlashCommand("display", "Displays a guide explaining how to use the bot.", false)]
    [SlashRequireGuild]
    public async Task DisplayAsync(
        InteractionContext context,
        [Option("channel", "The channel in which to display the information embed.")] DiscordChannel? channel = null
    )
    {
        channel ??= context.Channel;
        await _informationEmbedService.DisplayInChannelAsync(channel).ConfigureAwait(false);
        await context.CreateResponseAsync($"Embed has been to {channel.Mention}", true).ConfigureAwait(false);
    }

    [SlashCommand("addsection", "Adds a section to information embed.", false)]
    [SlashRequireGuild]
    public async Task AddSectionAsync(
        InteractionContext context
    )
    {
        DiscordGuild guild = context.Guild;
        (DiscordModalResponse response, string title, string body, int order) =
            await ShowSectionModal(context, "Add section", null);
        if (response != DiscordModalResponse.Success)
            return;

        await _informationEmbedService.AddSectionAsync(guild, title, body, order);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Section added");
        embed.AddField(title, body);

        await context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }

    [SlashCommand("editsection", "Edits a section of the information embed.", false)]
    [SlashRequireGuild]
    public async Task EditSectionAsync(
        InteractionContext context,
        [Option("section", "The section to edit.", true), Autocomplete(typeof(InformationEmbedSectionAutocompleteProvider))]
        string sectionInput
    )
    {
        var embed = new DiscordEmbedBuilder();

        if (!Guid.TryParse(sectionInput, out Guid sectionId))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"`{sectionInput}` is not a valid GUID.");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
        }

        DiscordGuild guild = context.Guild;
        if (!_informationEmbedService.TryGetInformationEmbedSection(guild, sectionId, out InformationEmbedSection? section))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"Section `{sectionInput}` was not found");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        (DiscordModalResponse response, string title, string body, int order) =
            await ShowSectionModal(context, "Edit section", section);
        if (response != DiscordModalResponse.Success)
            return;

        section.Title = title.Trim();
        section.Body = body.Trim();
        if (order > -1) section.Order = order;
        await _informationEmbedService.UpdateSectionAsync(section).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Section edited");
        embed.AddField(title, body);

        await context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }

    [SlashCommand("removesection", "Removes a section from the information embed.", false)]
    [SlashRequireGuild]
    public async Task RemoveSectionAsync(
        InteractionContext context,
        [Option("section", "The section to remove.", true), Autocomplete(typeof(InformationEmbedSectionAutocompleteProvider))]
        string sectionInput
    )
    {
        var embed = new DiscordEmbedBuilder();

        if (!Guid.TryParse(sectionInput, out Guid sectionId))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"`{sectionInput}` is not a valid GUID.");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
        }

        DiscordGuild guild = context.Guild;
        if (!_informationEmbedService.TryGetInformationEmbedSection(guild, sectionId, out InformationEmbedSection? section))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription($"Section `{sectionInput}` was not found");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _informationEmbedService.RemoveSectionAsync(section).ConfigureAwait(false);
        embed.WithColor(DiscordColor.Green);
        embed.WithDescription($"Section `{sectionInput}` was removed");
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }

    [SlashCommand("setintro", "Sets the introduction text of the information embed.", false)]
    [SlashRequireGuild]
    public async Task SetIntroAsync(InteractionContext context)
    {
        InformationEmbedSection introSection =
            await _informationEmbedService.GetIntroEmbedSectionAsync(context.Guild).ConfigureAwait(false);

        var builder = new DiscordModalBuilder(context.Client);
        builder.WithTitle("Set intro text");
        DiscordModalTextInput text = builder.AddInput(
            "Intro",
            initialValue: introSection.Body,
            isRequired: false,
            maxLength: 4000,
            inputStyle: TextInputStyle.Paragraph
        );

        DiscordModal modal = builder.Build();
        TimeSpan timeout = TimeSpan.FromMinutes(10);
        DiscordModalResponse response = await modal.RespondToAsync(context.Interaction, timeout).ConfigureAwait(false);

        if (response != DiscordModalResponse.Success)
            return;

        introSection.Body = text.Value;
        await _informationEmbedService.UpdateSectionAsync(introSection).ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Green);
        if (string.IsNullOrWhiteSpace(text.Value))
        {
            embed.WithTitle("Intro cleared");
        }
        else
        {
            embed.WithTitle("Intro updated");
            embed.WithDescription(text.Value);
        }

        await context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }

    private static async Task<(DiscordModalResponse Response, string Title, string Body, int Order)> ShowSectionModal(
        InteractionContext context,
        string modalTitle,
        InformationEmbedSection? existing
    )
    {
        var builder = new DiscordModalBuilder(context.Client);
        builder.WithTitle(modalTitle);

        DiscordModalTextInput title = builder.AddInput(
            "Title",
            initialValue: existing?.Title,
            isRequired: true,
            maxLength: 256,
            inputStyle: TextInputStyle.Short
        );
        DiscordModalTextInput order = builder.AddInput(
            "Order in embed",
            isRequired: true,
            initialValue: (existing?.Order ?? -1).ToString(),
            maxLength: 10,
            inputStyle: TextInputStyle.Short
        );
        DiscordModalTextInput body = builder.AddInput(
            "Body",
            isRequired: true,
            initialValue: existing?.Body,
            maxLength: 1024,
            inputStyle: TextInputStyle.Paragraph
        );

        DiscordModal modal = builder.Build();
        TimeSpan timeout = TimeSpan.FromMinutes(10);
        DiscordModalResponse response = await modal.RespondToAsync(context.Interaction, timeout).ConfigureAwait(false);

        if (!int.TryParse(order.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int orderValue))
            orderValue = -1;

        return (response, title.Value!, body.Value!, orderValue);
    }
}
