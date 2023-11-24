namespace Totem.Map.Builder;

internal sealed class ObservationBuilder : RuntimeBuilder<Observation>
{
    internal ObservationBuilder(EventBuilder e, ObserverRouteMethodBuilder? route, ObserverGivenMethodBuilder? given, ObserverWhenMethodBuilder? when)
    {
        Event = e;
        Route = route;
        Given = given;
        When = when;
    }

    internal EventBuilder Event { get; }
    internal ObserverRouteMethodBuilder? Route { get; }
    internal ObserverGivenMethodBuilder? Given { get; }
    internal ObserverWhenMethodBuilder? When { get; }

    protected override Observation BuildValue()
    {
        var observation = new Observation(Event.Value, Route?.Build(), Given?.Build(), When?.Build());

        if(Route is not null)
            Route.Value.Observation = observation;

        if(Given is not null)
            Given.Value.Observation = observation;

        if(When is not null)
            When.Value.Observation = observation;

        return observation;
    }
}
