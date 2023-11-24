namespace Totem.Map;

public sealed class ObserverGivenMethod : ObserverMethod
{
    internal delegate void CompiledCall(IEventObserver observer, IEventContext<IEvent> context);

    internal ObserverGivenMethod(MethodInfo info, EventParameter parameter) : base(info, parameter)
    {
        // (observer, context) => ((TObserver) observer).Given(<argument>)

        var observerParameter = Expression.Parameter(typeof(IEventObserver), "observer");
        var contextParameter = Expression.Parameter(typeof(IEventContext<IEvent>), "context");

        var observerCast = Expression.Convert(observerParameter, info.DeclaringType!);
        var call = Expression.Call(observerCast, info, parameter.ToArgument(contextParameter));
        var lambda = Expression.Lambda<CompiledCall>(call, observerParameter, contextParameter);

        Call = lambda.Compile();
    }

    internal CompiledCall Call { get; }
}
