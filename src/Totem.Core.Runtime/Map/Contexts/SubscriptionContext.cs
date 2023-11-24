namespace Totem.Map.Contexts;

internal sealed class SubscriptionContext<TSubscription> : MessageContext, ISubscriptionContext<TSubscription>
    where TSubscription : ISubscription
{
    internal SubscriptionContext(SubscriptionEnvelope envelope, SubscriptionType subscriptionType) : base(envelope)
    {
        Subscription = (TSubscription) envelope.Subscription;
        SubscriptionType = subscriptionType;
    }

    public new SubscriptionEnvelope Envelope => (SubscriptionEnvelope) base.Envelope;
    public TSubscription Subscription { get; }
    public SubscriptionInfo SubscriptionInfo => Envelope.SubscriptionInfo;
    public SubscriptionType SubscriptionType { get; }
    public Id SubscriberId => Envelope.Address.SubscriberId;
    public Id SubscriptionId => Envelope.Address.SubscriptionId;
    public SubscriptionAddress Address => Envelope.Address;
    public ISubscriptionReference? Reference { get; set; }
}
