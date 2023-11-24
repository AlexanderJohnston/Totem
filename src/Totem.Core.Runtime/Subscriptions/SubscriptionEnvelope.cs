namespace Totem.Subscriptions;

public sealed class SubscriptionEnvelope : MessageEnvelope
{
    public SubscriptionEnvelope(ISubscription subscription, SubscriptionAddress address, EnvelopeInfo info) : base(info)
    {
        Subscription = subscription;
        SubscriptionInfo = SubscriptionInfo.From(subscription.GetType());
        Address = address;
    }

    public SubscriptionEnvelope(ISubscription subscription, SubscriptionAddress address, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(subscription, address, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public ISubscription Subscription { get; }
    public SubscriptionInfo SubscriptionInfo { get; }
    public Type SubscriptionType => SubscriptionInfo.DeclaredType;
    public Id SubscriptionId => Info.MessageId;
    public SubscriptionAddress Address { get; }
}
