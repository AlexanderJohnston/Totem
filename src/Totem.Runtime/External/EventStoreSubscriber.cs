using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using Totem.Events;

namespace Totem.External
{
    public abstract class EventStoreSubscriber<T> : SubscribesEvent<T> where T : SubscriptionLink
    {
        protected EventStoreClient Client { get; }
        protected EventPosition? LastProcessed { get; set; }

        protected EventStoreSubscriber(EventStoreClient client, T subscription, IEventPipeline consumptionPipe) 
            : base(subscription, consumptionPipe)
        {
            Client = client;
        }
    }
}
