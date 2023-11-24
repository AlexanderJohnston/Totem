namespace Totem.Commands;

public sealed class CommandInfo : MessageInfo
{
    static readonly MessageInfoCache<CommandInfo> _cache = new();

    CommandInfo(Type declaredType) : base(declaredType)
    { }

    public static bool TryFrom(Type type, [NotNullWhen(true)] out CommandInfo? info)
    {
        if(_cache.TryGetValue(type, out info))
        {
            return true;
        }

        if(type is not null && type.IsConcreteClass() && typeof(ICommand).IsAssignableFrom(type))
        {
            info = new CommandInfo(type);

            _cache.Add(info);

            return true;
        }

        return false;
    }

    public static CommandInfo From(Type type)
    {
        if(!TryFrom(type, out var info))
            throw new ArgumentException($"Expected type to be a public, non-abstract class implementing {typeof(ICommand)}", nameof(type));

        return info;
    }
}
