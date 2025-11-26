namespace Totem.Subscriptions;

public sealed class SubscriptionAddress : IEquatable<SubscriptionAddress>
{
    public SubscriptionAddress(Id subscriberId, Id subscriptionId)
    {
        SubscriberId = subscriberId;
        SubscriptionId = subscriptionId;
    }

    public Id SubscriberId { get; }
    public Id SubscriptionId { get; }

    public override string ToString() =>
        $"{SubscriberId.ToShortString()}.{SubscriptionId.ToShortString()}";

    public override bool Equals(object? obj) =>
        obj is SubscriptionAddress other && Equals(other);

    public bool Equals(SubscriptionAddress? other) =>
        other is not null
        && SubscriberId == other.SubscriberId
        && SubscriptionId == other.SubscriptionId;

    public override int GetHashCode() =>
        HashCode.Combine(SubscriberId, SubscriptionId);

    public static bool operator ==(SubscriptionAddress? x, SubscriptionAddress? y) => EqualityComparer<SubscriptionAddress>.Default.Equals(x, y);
    public static bool operator !=(SubscriptionAddress? x, SubscriptionAddress? y) => !(x == y);
}
