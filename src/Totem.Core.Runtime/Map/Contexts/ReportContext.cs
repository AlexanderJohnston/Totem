namespace Totem.Map.Contexts;

internal sealed class ReportContext<TEvent> : ObserverContext<TEvent>, IReportContext<TEvent>
    where TEvent : IEvent
{
    internal ReportContext(EventEnvelope envelope, EventType eventType, ObserverRoute route)
        : base(envelope, eventType, route.Observation)
    {
        ReportKey = new(route.Observer.DeclaredType, route.ObserverId);
        ReportType = (ReportType) route.Observer;
        ReportId = route.ObserverId;
    }

    public TimelineKey ReportKey { get; }
    public ReportType ReportType { get; }
    public Id ReportId { get; }

    public override string ToString() =>
        $"{base.ToString()} => {ReportKey}";
}
