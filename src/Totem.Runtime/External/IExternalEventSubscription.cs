using Totem.Core;

namespace Totem.External;

public interface IExternalEventSubscription
{
    Task  Publish(IEventEnvelope envelope);
}
