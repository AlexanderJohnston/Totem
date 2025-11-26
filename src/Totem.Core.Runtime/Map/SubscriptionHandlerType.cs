namespace Totem.Map;

public sealed class SubscriptionHandlerType : MessageHandlerType
{
    internal SubscriptionHandlerType(Type declaredType, Type serviceType, SubscriptionType subscription) : base(declaredType, serviceType) =>
        Subscription = subscription;

    public SubscriptionType Subscription { get; }
}
