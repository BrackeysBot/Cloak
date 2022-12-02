namespace Cloak.Data;

internal sealed class SelfRole : IEquatable<SelfRole>
{
    /// <summary>
    ///     Gets or sets the guild ID.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the group of this role.
    /// </summary>
    /// <value>The group.</value>
    public string? Group { get; set; }

    /// <summary>
    ///     Gets or sets the role ID.
    /// </summary>
    /// <value>The role ID.</value>
    public ulong RoleId { get; set; }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="SelfRole" /> instances are equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true" /> if the two instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(SelfRole? left, SelfRole? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="SelfRole" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true" /> if the two instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(SelfRole? left, SelfRole? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether this instance and another instance of <see cref="SelfRole" /> are equal.
    /// </summary>
    /// <param name="other">The other instance</param>
    /// <returns>
    ///     <see langword="tru" /> if this instance is equal to <paramref name="other" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(SelfRole? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return GuildId == other.GuildId && RoleId == other.RoleId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is SelfRole other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(GuildId, RoleId);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
