namespace Totem.Map;

public abstract class ObserverType : TimelineType
{
    internal ObserverType(Type declaredType, bool isSingleInstance) : base(declaredType, isSingleInstance)
    { }

    public ObservationCollection Observations { get; } = new();

    internal ObserverRoute RouteToSingleInstance(IEventContext<IEvent> context)
    {
        if(!Observations.TryGet(context.EventType, out var observation))
            throw new Exception($"Expected observer to observe {context.EventType}: {this}");

        if(SingleInstanceId is null)
            throw new Exception($"Expected observer to be single-instance: {this}");

        return new ObserverRoute(observation, SingleInstanceId);
    }

    internal IEnumerable<ObserverRoute> RouteToObservations(IEventContext<IEvent> context)
    {
        if(Observations.TryGet(context.EventType, out var observation))
        {
            foreach(var route in observation.CallRoute(context))
            {
                yield return route;
            }
        }
    }

    internal void CallGivenIfDefined(IEventObserver observer, IEventContext<IEvent> context)
    {
        if(Observations.TryGet(context.EventType, out var observation) && observation.Given is not null)
        {
            observation.Given.Call(observer, context);
        }
    }
}
