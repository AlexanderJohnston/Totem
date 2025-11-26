namespace Totem;

public interface ISubscriptionContext<out TSubscription> : IMessageContext
    where TSubscription : ISubscription
{
    new SubscriptionEnvelope Envelope { get; }
    TSubscription Subscription { get; }
    SubscriptionInfo SubscriptionInfo { get; }
    SubscriptionType SubscriptionType { get; }
    Id SubscriptionId { get; }
    Id SubscriberId { get; }
    SubscriptionAddress Address { get; }
    ISubscriptionReference? Reference { get; set; }
}
