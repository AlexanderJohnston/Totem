namespace Totem.Map;

public abstract class RuntimeMethodParameter
{
    internal RuntimeMethodParameter(ParameterInfo info, MessageType message)
    {
        Info = info;
        Message = message;
    }

    public ParameterInfo Info { get; }
    public MessageType Message { get; }

    public override string ToString() =>
        Info.ToString();
}
