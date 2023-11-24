namespace Totem.Map;

public abstract class MessageHandlerType : RuntimeType
{
    internal MessageHandlerType(Type declaredType, Type serviceType) : base(declaredType) =>
        ServiceType = serviceType;

    public Type ServiceType { get; }
}
