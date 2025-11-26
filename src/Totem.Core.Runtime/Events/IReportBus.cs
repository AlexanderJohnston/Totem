namespace Totem.Events;

public interface IReportBus
{
    Task PublishAsync(EventEnvelope newEvent, IEnumerable<ObserverRoute> routes, CancellationToken cancellationToken);
}
