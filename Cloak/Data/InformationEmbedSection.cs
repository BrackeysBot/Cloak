using System.ComponentModel.DataAnnotations;

namespace Cloak.Data;

/// <summary>
///     Represents a section on the information embed.
/// </summary>
internal sealed class InformationEmbedSection : IEquatable<InformationEmbedSection>
{
    /// <summary>
    ///     Gets the body of the section.
    /// </summary>
    /// <value>The body.</value>
    [MaxLength(1024)]
    public string? Body { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the guild ID.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the section. 
    /// </summary>
    /// <value>The section ID.</value>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the display order of the section.
    /// </summary>
    /// <value>The display order.</value>
    public int Order { get; set; }

    /// <summary>
    ///     Gets the title of the section.
    /// </summary>
    /// <value>The title.</value>
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public static bool operator ==(InformationEmbedSection? left, InformationEmbedSection? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(InformationEmbedSection? left, InformationEmbedSection? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public bool Equals(InformationEmbedSection? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is InformationEmbedSection other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }
}
