namespace Totem;

public interface IReportContext<out TEvent> : IObserverContext<TEvent>
    where TEvent : IEvent
{
    TimelineKey ReportKey { get; }
    ReportType ReportType { get; }
    Id ReportId { get; }
}
