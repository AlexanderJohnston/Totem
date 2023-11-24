namespace Totem.Map.Builder;

internal sealed class SubscriptionHandlerBuilder : RuntimeTypeBuilder<SubscriptionHandlerType>
{
    internal SubscriptionHandlerBuilder(Type declaredType) : base(declaredType)
    { }

    internal SubscriptionBuilder Subscription { get; private set; } = null!;
    internal Type ServiceType { get; private set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var subscriptionType = DeclaredType.GetImplementedInterfaceGenericArguments(typeof(ISubscriptionHandler<>)).Single();

        if(!map.Messages.Subscriptions.TryGet(subscriptionType, out var subscription))
        {
            SetError(BuildErrors.HandlerMessageNotFound, new { subscriptionType });
            return;
        }

        if(subscription.Handler is not null)
        {
            SetError(BuildErrors.HandlerDuplicated, new { subscription.Handler });
            return;
        }

        Subscription = subscription;
        Subscription.Handler = this;
        ServiceType = typeof(ISubscriptionHandler<>).MakeGenericType(subscriptionType);
    }

    internal override void Validate()
    {
        if(Subscription is null)
        {
            SetError(BuildErrors.HandlerMessageHasError);
        }
    }

    protected override SubscriptionHandlerType BuildValue()
    {
        var handler = new SubscriptionHandlerType(DeclaredType, ServiceType, Subscription.Value);

        Subscription.Value.Handler = handler;

        return handler;
    }
}
