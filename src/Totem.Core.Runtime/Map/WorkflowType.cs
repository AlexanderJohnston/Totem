namespace Totem.Map;

public sealed class WorkflowType : ObserverType
{
    internal WorkflowType(Type declaredType, bool isSingleInstance) : base(declaredType, isSingleInstance)
    { }
}
