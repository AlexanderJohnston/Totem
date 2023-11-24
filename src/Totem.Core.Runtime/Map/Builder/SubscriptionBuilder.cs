namespace Totem.Map.Builder;

internal sealed class SubscriptionBuilder : RuntimeTypeBuilder<SubscriptionType>
{
    internal SubscriptionBuilder(Type declaredType) : base(declaredType)
    { }

    internal SubscriptionInfo Info { get; private set; } = null!;
    internal SubscriptionHandlerBuilder Handler { get; set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        if(!SubscriptionInfo.TryFrom(DeclaredType, out var info))
        {
            SetError(BuildErrors.MessageInfoNotFound);
            return;
        }

        Info = info;
    }

    internal override void Validate()
    {
        if(Handler is null)
        {
            SetError(BuildErrors.HandlerNotFound);
        }
    }

    protected override SubscriptionType BuildValue() =>
        new(Info);
}
