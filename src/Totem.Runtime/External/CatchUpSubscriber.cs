using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using Totem.Events;

namespace Totem.External
{
    public abstract class CatchUpSubscriber<T> : EventStoreSubscriber<T>
        where T : CatchUpSubscription
    {
        protected IStoreCheckpoints CheckpointStore { get; }
        CheckpointCommitHandler CheckpointCommitHandler { get; }


        protected CatchUpSubscriber(
            EventStoreClient client, 
            T subscription, 
            IEventPipeline consumptionPipe
            ) : base(client, subscription, consumptionPipe)
        {
            Check
        }
    }
}
