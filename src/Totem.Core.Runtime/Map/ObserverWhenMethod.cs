namespace Totem.Map;

public sealed class ObserverWhenMethod : ObserverMethod
{
    internal delegate void CompiledCall(IEventObserver observer, IEventContext<IEvent> context);

    readonly CompiledCall _call;

    internal ObserverWhenMethod(MethodInfo info, EventParameter parameter) : base(info, parameter)
    {
        // (observer, context) => ((TObserver) observer).info(<argument>)

        var observerParameter = Expression.Parameter(typeof(IEventObserver), "observer");
        var contextParameter = Expression.Parameter(typeof(IEventContext<IEvent>), "context");

        var observerCast = Expression.Convert(observerParameter, info.DeclaringType!);
        var call = Expression.Call(observerCast, info, parameter.ToArgument(contextParameter));
        var lambda = Expression.Lambda<CompiledCall>(call, observerParameter, contextParameter);

        _call = lambda.Compile();
    }

    internal void Call(IEventObserver observer, IEventContext<IEvent> context)
    {
        _call(observer, context);

        foreach(var error in observer.Errors)
        {
            context.Errors.Add(error);
        }
    }
}
