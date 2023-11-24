namespace Totem.InMemory.Events;

public interface IInMemoryEventBus
{
    void Publish(IReadOnlyList<EventEnvelope> newEvents);
}
