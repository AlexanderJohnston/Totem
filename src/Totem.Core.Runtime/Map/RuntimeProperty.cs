namespace Totem.Map;

public abstract class RuntimeProperty
{
    internal RuntimeProperty(PropertyInfo info) =>
        Info = info;

    public PropertyInfo Info { get; }

    public override string ToString() =>
        Info.ToString() ?? $"{Info.DeclaringType}.{Info.Name}";
}
