using EventStore.Client;
using Totem.Events;

namespace Totem.External
{
    public record AllSusbcription : CatchUpSubscription
    {
        public IEventFilter? EventFilter { get; set; }
        public IEventPipeline EventPipeline { get; set; }
    }
}
