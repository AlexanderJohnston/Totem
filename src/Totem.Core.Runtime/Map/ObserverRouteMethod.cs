namespace Totem.Map;

public sealed class ObserverRouteMethod : ObserverMethod
{
    delegate Id CompiledCall(IEventContext<IEvent> context);
    delegate IEnumerable<Id> CompiledCallMany(IEventContext<IEvent> context);

    readonly CompiledCall _call = null!;
    readonly CompiledCallMany _callMany = null!;

    internal ObserverRouteMethod(MethodInfo info, EventParameter parameter, bool returnsMany) : base(info, parameter)
    {
        ReturnsMany = returnsMany;

        var contextParameter = Expression.Parameter(typeof(IEventContext<IEvent>), "context");
        var call = Expression.Call(info, parameter.ToArgument(contextParameter));

        if(!returnsMany)
        {
            // e => info(e)

            _call = Expression.Lambda<CompiledCall>(call, contextParameter).Compile();
        }
        else if(info.ReturnType == typeof(IEnumerable<Id>))
        {
            // e => info(e)

            _callMany = Expression.Lambda<CompiledCallMany>(call, contextParameter).Compile();
        }
        else
        {
            // e => (IEnumerable<Id>) info(e)

            var castCall = Expression.Convert(call, typeof(IEnumerable<Id>));

            _callMany = Expression.Lambda<CompiledCallMany>(castCall, contextParameter).Compile();
        }
    }

    public bool ReturnsMany { get; }

    internal IEnumerable<ObserverRoute> Call(IEventContext<IEvent> context)
    {
        if(_call is not null)
        {
            yield return new(Observation, _call(context));
        }
        else
        {
            foreach(var id in _callMany!(context))
            {
                yield return new(Observation, id);
            }
        }
    }
}
