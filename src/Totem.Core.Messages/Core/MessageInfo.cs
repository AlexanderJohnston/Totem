namespace Totem.Core;

public abstract class MessageInfo
{
    internal MessageInfo(Type declaredType)
    {
        DeclaredType = declaredType;
        ExternalType = declaredType.GetCustomAttribute<ExternalTypeAttribute>()?.ExternalType ?? declaredType.Name;
    }

    public Type DeclaredType { get; }
    public string ExternalType { get; }

    public override string ToString() =>
        ExternalType;
}
