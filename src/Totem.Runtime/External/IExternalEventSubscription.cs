using Totem.Core;

namespace Totem.External;

public interface IExternalEventSubscription
{
    void Publish(IEventEnvelope envelope);
}
