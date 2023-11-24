namespace Totem.Map;

public abstract class RuntimeType
{
    internal RuntimeType(Type declaredType) =>
        DeclaredType = declaredType;

    public Type DeclaredType { get; }

    public override string ToString() =>
        DeclaredType.ToString();

    public bool IsAssignableTo(Type type) =>
        type.IsAssignableFrom(DeclaredType);

    public bool IsAssignableTo<T>() =>
        typeof(T).IsAssignableFrom(DeclaredType);
}
