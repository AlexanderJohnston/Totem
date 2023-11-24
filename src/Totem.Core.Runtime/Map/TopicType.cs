namespace Totem.Map;

public sealed class TopicType : TimelineType
{
    internal TopicType(Type declaredType, bool isSingleInstance, RuntimeMethodCollection<TopicGivenMethod> givens)
        : base(declaredType, isSingleInstance)
    {
        Givens = givens;
    }

    public RuntimeTypeCollection<CommandType> Commands { get; } = new();
    public RuntimeMethodCollection<TopicGivenMethod> Givens { get; }

    internal TopicRoute CallSingleInstanceRoute()
    {
        if(!IsSingleInstance)
            throw new Exception($"Expected topic to be single-instance: {this}");

        return new(this, SingleInstanceId);
    }

    internal void CallGivenIfDefined(ITopic topic, IEventContext<IEvent> context)
    {
        if(Givens.TryGet(context.EventType, out var given))
        {
            given.Call(topic, context);
        }
    }
}
