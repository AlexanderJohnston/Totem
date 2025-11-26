namespace Totem.Map;

public sealed class SubscriptionType : MessageType
{
    internal SubscriptionType(SubscriptionInfo info) : base(info)
    { }

    public new SubscriptionInfo Info => (SubscriptionInfo) base.Info;
    public SubscriptionHandlerType Handler { get; internal set; } = null!;
}
