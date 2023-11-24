namespace Totem.Subscriptions;

public sealed class TspSubscriptionContext<TSubscription> : MessageContext, ITspSubscriptionContext<TSubscription>
    where TSubscription : ITspSubscription
{
    public TspSubscriptionContext(TspSubscriptionEnvelope envelope, SubscriptionType subscriptionType) : base(envelope)
    {
        Subscription = (TSubscription) envelope.Subscription;
        SubscriptionType = subscriptionType;
    }

    public new TspSubscriptionEnvelope Envelope => (TspSubscriptionEnvelope) base.Envelope;
    public TSubscription Subscription { get; }
    public SubscriptionInfo SubscriptionInfo => Envelope.SubscriptionInfo;
    public SubscriptionType SubscriptionType { get; }
    public Id SubscriptionId => Envelope.SubscriptionId;
}
