namespace Totem.Map;

public abstract class MessageType : RuntimeType
{
    internal MessageType(MessageInfo info) : base(info.DeclaredType) =>
        Info = info;

    public MessageInfo Info { get; }
    public string ExternalType => Info.ExternalType;

    public override string ToString() =>
        ExternalType;
}
