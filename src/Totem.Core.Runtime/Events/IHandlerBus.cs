namespace Totem.Events;

public interface IHandlerBus
{
    Task PublishAsync(EventEnvelope newEvent, CancellationToken cancellationToken);
}
