using System.Diagnostics.CodeAnalysis;
using Cloak.Data;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cloak.Services;

/// <summary>
///     Represents a service which manages the information embed.
/// </summary>
internal sealed class InformationEmbedService : BackgroundService
{
    private const string IntroSectionName = "#INTRO";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly Dictionary<DiscordGuild, List<InformationEmbedSection>> _informationEmbedSections = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InformationEmbedService" /> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="discordClient">The Discord client.</param>
    public InformationEmbedService(IServiceScopeFactory scopeFactory, DiscordClient discordClient)
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Adds a new section to the information embed.
    /// </summary>
    /// <param name="guild">The guild whose information embed to update.</param>
    /// <param name="title">The title of the section.</param>
    /// <param name="body">The body of the section.</param>
    /// <param name="order">The display order of the section.</param>
    /// <returns>The newly-created <see cref="InformationEmbedSection" />.</returns>
    public async Task<InformationEmbedSection> AddSectionAsync(DiscordGuild guild, string title, string body, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(body);

        var section = new InformationEmbedSection
        {
            Id = Guid.NewGuid(),
            GuildId = guild.Id,
            Title = title.Length > 256 ? title[..256] : title,
            Body = body.Length > 1024 ? body[..1024] : body,
            Order = order
        };

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        EntityEntry<InformationEmbedSection> entry = await context.AddAsync(section).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
        section = entry.Entity;

        if (!_informationEmbedSections.TryGetValue(guild, out List<InformationEmbedSection>? sections))
        {
            sections = new List<InformationEmbedSection>();
            _informationEmbedSections[guild] = sections;
        }

        sections.Add(section);
        return section;
    }

    /// <summary>
    ///     Displays the information embed in the specified channel.
    /// </summary>
    /// <param name="channel">The channel in which to display the embed.</param>
    /// <exception cref="ArgumentException"><paramref name="channel" /> is <see langword="null" />.</exception>
    public Task DisplayInChannelAsync(DiscordChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        if (channel.Guild is not { } guild) throw new ArgumentException("Channel must be in a guild", nameof(channel));

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.CornflowerBlue);
        embed.WithTitle("The Roles");

        if (TryGetInformationEmbedSection(guild, IntroSectionName, out InformationEmbedSection? informationEmbedSection))
            embed.WithDescription(informationEmbedSection.Body);

        foreach (InformationEmbedSection section in GetInformationEmbedSections(channel.Guild, false).OrderBy(s => s.Order))
            embed.AddField(section.Title, section.Body);

        return channel.SendMessageAsync(embed);
    }

    /// <summary>
    ///     Returns the intro section for the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose embed sections to search.</param>
    /// <returns>The intro section.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public async Task<InformationEmbedSection> GetIntroEmbedSectionAsync(DiscordGuild guild)
    {
        if (TryGetInformationEmbedSection(guild, IntroSectionName, out InformationEmbedSection? section))
            return section;
        
        return await AddSectionAsync(guild, IntroSectionName, string.Empty).ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempts to find a section by its ID.
    /// </summary>
    /// <param name="guild">The guild whose embed sections to search.</param>
    /// <param name="id">The ID of the section to retrieve.</param>
    /// <param name="section">
    ///     When this method returns, contains the section whose ID is equal to <paramref name="id" />, if one was
    ///     found; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the section was found; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public bool TryGetInformationEmbedSection(
        DiscordGuild guild,
        Guid id,
        [NotNullWhen(true)] out InformationEmbedSection? section
    )
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (!_informationEmbedSections.TryGetValue(guild, out List<InformationEmbedSection>? informationEmbedSections))
        {
            section = null;
            return false;
        }

        for (var index = 0; index < informationEmbedSections.Count; index++)
        {
            section = informationEmbedSections[index];
            if (section.Id == id)
                return true;
        }

        section = null;
        return false;
    }

    /// <summary>
    ///     Attempts to find a section by its name.
    /// </summary>
    /// <param name="guild">The guild whose embed sections to search.</param>
    /// <param name="sectionName">The name of the section to retrieve.</param>
    /// <param name="section">
    ///     When this method returns, contains the section whose name is equal to <paramref name="sectionName" />, if one was
    ///     found; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if the section was found; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public bool TryGetInformationEmbedSection(
        DiscordGuild guild,
        string sectionName,
        [NotNullWhen(true)] out InformationEmbedSection? section
    )
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(sectionName);

        section = null;
        if (!_informationEmbedSections.TryGetValue(guild, out List<InformationEmbedSection>? informationEmbedSections))
            return false;

        for (var index = 0; index < informationEmbedSections.Count; index++)
        {
            if (string.Equals(informationEmbedSections[index].Title, sectionName, StringComparison.Ordinal))
            {
                section = informationEmbedSections[index];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Gets all the embed sections in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose embed sections to retrieve.</param>
    /// <param name="includeIntro">
    ///     <see langword="true" /> to include the intro section in the result; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>A read-only view of the embed section.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public IReadOnlyList<InformationEmbedSection> GetInformationEmbedSections(DiscordGuild guild, bool includeIntro)
    {
        ArgumentNullException.ThrowIfNull(guild);
        if (!_informationEmbedSections.TryGetValue(guild, out List<InformationEmbedSection>? informationEmbedSections))
            return ArraySegment<InformationEmbedSection>.Empty;

        if (!includeIntro)
        {
            for (var index = 0; index < informationEmbedSections.Count; index++)
            {
                if (string.Equals(informationEmbedSections[index].Title, IntroSectionName, StringComparison.Ordinal))
                {
                    informationEmbedSections.RemoveAt(index);
                    break;
                }
            }
        }

        return informationEmbedSections.AsReadOnly();
    }

    /// <summary>
    ///     Removes an information embed section.
    /// </summary>
    /// <param name="section">The section to remove.</param>
    /// <exception cref="ArgumentException"><paramref name="section" /> is <see langword="null" />.</exception>
    public async Task RemoveSectionAsync(InformationEmbedSection section)
    {
        ArgumentNullException.ThrowIfNull(section);
        DiscordGuild guild = await _discordClient.GetGuildAsync(section.GuildId).ConfigureAwait(false);
        if (_informationEmbedSections.TryGetValue(guild, out List<InformationEmbedSection>? sections))
            sections.Remove(section);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        context.Remove(section);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates a section in the database.
    /// </summary>
    /// <param name="section">The section to update.</param>
    public async Task UpdateSectionAsync(InformationEmbedSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        context.Update(section);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        return Task.CompletedTask;
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        return UpdateFromDatabaseAsync(e.Guild);
    }

    private async Task UpdateFromDatabaseAsync(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CloakContext>();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        if (!_informationEmbedSections.TryGetValue(guild, out List<InformationEmbedSection>? sections))
        {
            sections = new List<InformationEmbedSection>();
            _informationEmbedSections[guild] = sections;
        }

        await foreach (InformationEmbedSection section in context.InformationEmbedSections)
        {
            if (section.GuildId == guild.Id)
                sections.Add(section);
        }
    }
}
