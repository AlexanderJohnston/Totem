namespace Totem.Events;

public interface IWorkflowBus
{
    Task PublishAsync(EventEnvelope newEvent, IEnumerable<ObserverRoute> routes, CancellationToken cancellationToken);
}
