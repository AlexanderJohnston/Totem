namespace Totem.Reports;

public interface IReportPipeline
{
    Task<IReportContext<IEvent>> RunAsync(EventEnvelope envelope, ObserverRoute route, CancellationToken cancellationToken);
}
