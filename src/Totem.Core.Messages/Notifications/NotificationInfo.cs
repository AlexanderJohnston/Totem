namespace Totem.Notifications;

public sealed class NotificationInfo : MessageInfo
{
    static readonly MessageInfoCache<NotificationInfo> _cache = new();

    NotificationInfo(Type declaredType) : base(declaredType)
    { }

    public static bool TryFrom(Type type, [NotNullWhen(true)] out NotificationInfo? info)
    {
        if(_cache.TryGetValue(type, out info))
        {
            return true;
        }

        if(type is not null && type.IsConcreteClass() && typeof(INotification).IsAssignableFrom(type))
        {
            info = new NotificationInfo(type);

            _cache.Add(info);

            return true;
        }

        return false;
    }

    public static NotificationInfo From(Type type)
    {
        if(!TryFrom(type, out var info))
            throw new ArgumentException($"Expected type to be a public, non-abstract class implementing {typeof(INotification)}", nameof(type));

        return info;
    }
}
