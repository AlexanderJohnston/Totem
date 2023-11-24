namespace Totem.Map.Summary;

public sealed class ObservationSummary
{
    public ObservationSummary(Id observerTypeId, Id eventTypeId, ObserverMethodSummary? route, EventMethodSummary? given, ObserverMethodSummary? when)
    {
        ObserverTypeId = observerTypeId;
        EventTypeId = eventTypeId;
        Route = route;
        Given = given;
        When = when;
    }

    public Id ObserverTypeId { get; }
    public Id EventTypeId { get; }
    public ObserverMethodSummary? Route { get; }
    public EventMethodSummary? Given { get; }
    public ObserverMethodSummary? When { get; }
}
