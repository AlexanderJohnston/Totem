namespace Totem.Map;

public sealed class EventType : MessageType
{
    internal EventType(EventInfo info) : base(info)
    { }

    public new EventInfo Info => (EventInfo) base.Info;
    public EventHandlerType? Handler { get; internal set; }
    public ObservationCollection ReportObservations { get; } = new();
    public ObservationCollection WorkflowObservations { get; } = new();

    internal IEnumerable<ObserverRoute> RouteReports(IEventContext<IEvent> context) =>
        ReportObservations.SelectMany(x => x.CallRoute(context));

    internal IEnumerable<ObserverRoute> RouteWorkflows(IEventContext<IEvent> context) =>
        WorkflowObservations.SelectMany(x => x.CallRoute(context));
}
