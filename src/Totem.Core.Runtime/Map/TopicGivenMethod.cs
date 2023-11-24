namespace Totem.Map;

public sealed class TopicGivenMethod : RuntimeMethod
{
    internal delegate void CompiledCall(ITopic topic, IEventContext<IEvent> context);

    internal TopicGivenMethod(MethodInfo info, EventParameter parameter) : base(info, parameter)
    {
        // (topic, context) => ((TTopic) topic).Given(<argument>)

        var topicParameter = Expression.Parameter(typeof(ITopic), "topic");
        var contextParameter = Expression.Parameter(typeof(IEventContext<IEvent>), "context");

        var topicCast = Expression.Convert(topicParameter, info.DeclaringType!);
        var call = Expression.Call(topicCast, info, parameter.ToArgument(contextParameter));
        var lambda = Expression.Lambda<CompiledCall>(call, topicParameter, contextParameter);

        Call = lambda.Compile();
    }

    public new EventParameter Parameter => (EventParameter) base.Parameter;
    internal CompiledCall Call { get; }
}
