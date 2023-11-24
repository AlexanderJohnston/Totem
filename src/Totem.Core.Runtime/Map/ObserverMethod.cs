namespace Totem.Map;

public abstract class ObserverMethod : RuntimeMethod
{
    internal ObserverMethod(MethodInfo info, EventParameter parameter) : base(info, parameter)
    { }

    public new EventParameter Parameter => (EventParameter) base.Parameter;
    public Observation Observation { get; internal set; } = null!;
}
