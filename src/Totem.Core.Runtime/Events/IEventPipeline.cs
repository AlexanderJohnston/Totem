namespace Totem.Events;

public interface IEventPipeline
{
    Task<IEventContext<IEvent>> RunAsync(EventEnvelope envelope, CancellationToken cancellationToken);
}
