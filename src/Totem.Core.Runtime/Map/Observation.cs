namespace Totem.Map;

public sealed class Observation
{
    internal Observation(EventType e, ObserverRouteMethod? route, ObserverGivenMethod? given, ObserverWhenMethod? when)
    {
        Event = e;
        Route = route;
        Given = given;
        When = when;
    }

    public ObserverType Observer { get; internal set; } = null!;
    public EventType Event { get; }
    public ObserverRouteMethod? Route { get; }
    public ObserverGivenMethod? Given { get; }
    public ObserverWhenMethod? When { get; }

    public override string ToString() =>
        $"{Event} => {Observer}";

    internal IEnumerable<ObserverRoute> CallRoute(IEventContext<IEvent> context)
    {
        if(Observer.SingleInstanceId is not null)
        {
            yield return Observer.RouteToSingleInstance(context);
            yield break;
        }

        foreach(var route in Route!.Call(context))
        {
            yield return route;
        }
    }

    internal void CallGivenIfDefined(IEventObserver observer, IEventContext<IEvent> context) =>
        Given?.Call(observer, context);

    internal void CallWhenIfDefined(IEventObserver observer, IEventContext<IEvent> context) =>
        When?.Call(observer, context);
}
