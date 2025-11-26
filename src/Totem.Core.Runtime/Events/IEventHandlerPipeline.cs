namespace Totem.Events;

public interface IEventHandlerPipeline
{
    Task<IEventHandlerContext<IEvent>> RunAsync(EventEnvelope envelope, CancellationToken cancellationToken);
}
