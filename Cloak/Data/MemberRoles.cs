namespace Cloak.Data;

/// <summary>
///     Represents a cache of persistent member roles.
/// </summary>
internal sealed class MemberRoles : IEquatable<MemberRoles>
{
    /// <summary>
    ///     Gets or sets the guild ID.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the set of persistent role IDs that the user should maintain upon rejoining.
    /// </summary>
    /// <value>The persistent role IDs.</value>
    public IReadOnlyList<ulong> RoleIds { get; set; } = ArraySegment<ulong>.Empty;

    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    /// <value>The user ID.</value>
    public ulong UserId { get; set; }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="MemberRoles" /> instances are equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true" /> if the two instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(MemberRoles? left, MemberRoles? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="MemberRoles" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true" /> if the two instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(MemberRoles? left, MemberRoles? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether this instance and another instance of <see cref="MemberRoles" /> are
    ///     equal.
    /// </summary>
    /// <param name="other">The other instance</param>
    /// <returns>
    ///     <see langword="tru" /> if this instance is equal to <paramref name="other" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(MemberRoles? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return GuildId == other.GuildId && UserId == other.UserId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is MemberRoles other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(GuildId, UserId);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
