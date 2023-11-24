namespace Totem.Map;

public abstract class RuntimeMethod
{
    public const string Route = nameof(Route);
    public const string Given = nameof(Given);
    public const string When = nameof(When);

    internal RuntimeMethod(MethodInfo info, RuntimeMethodParameter parameter)
    {
        Info = info;
        Parameter = parameter;
    }

    public MethodInfo Info { get; }
    public RuntimeMethodParameter Parameter { get; }

    public override string ToString() =>
        Info.ToString() ?? $"{Info.DeclaringType}.{Info.Name}";
}
