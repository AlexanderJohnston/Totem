using Totem.Core;

namespace Totem.InMemory;

public interface IInMemoryEventSubscription
{
    Task Publish(IEventEnvelope envelope);
}
