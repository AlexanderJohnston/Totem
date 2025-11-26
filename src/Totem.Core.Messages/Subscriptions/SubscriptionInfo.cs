namespace Totem.Subscriptions;

public sealed class SubscriptionInfo : MessageInfo
{
    static readonly MessageInfoCache<SubscriptionInfo> _cache = new();

    SubscriptionInfo(Type declaredType) : base(declaredType)
    { }

    public static bool TryFrom(Type type, [NotNullWhen(true)] out SubscriptionInfo? info)
    {
        if(_cache.TryGetValue(type, out info))
        {
            return true;
        }

        if(type is not null && type.IsConcreteClass() && typeof(ISubscription).IsAssignableFrom(type))
        {
            info = new SubscriptionInfo(type);

            _cache.Add(info);

            return true;
        }

        return false;
    }

    public static SubscriptionInfo From(Type type)
    {
        if(!TryFrom(type, out var info))
            throw new ArgumentException($"Expected type to be a public, non-abstract class implementing {typeof(ISubscription)}", nameof(type));

        return info;
    }
}
