namespace Totem;

public interface ITspSubscriptionContext<out TSubscription> : IMessageContext
    where TSubscription : ITspSubscription
{
    new TspSubscriptionEnvelope Envelope { get; }
    TSubscription Subscription { get; }
    SubscriptionInfo SubscriptionInfo { get; }
    SubscriptionType SubscriptionType { get; }
    Id SubscriptionId { get; }
}
