using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Totem.Events;

namespace Totem.External
{
    public delegate void OnSubscribed(string subscriptionId);
    public delegate void OnDropped(string subscriptionId, string reason, Exception? exception);

    internal interface ISubscribeMessage
    {
        ValueTask Subscribe(OnSubscribed observe, OnDropped release, CancellationToken cancel);
    }

    public abstract class SubscribesEvent<T> : ISubscribeMessage where T : SubscriptionLink
    {
        protected internal T _sub { get; }
        public bool Started { get; set; }
        protected CancellationTokenSource Halt { get; } = new();
        
        OnSubscribed? _observed;
        OnDropped? _released;
        internal IEventPipeline _consumptionPipe;

        protected SubscribesEvent(T subscription, IEventPipeline consumptionPipe)
        {
            _consumptionPipe = consumptionPipe;
            _sub = subscription;
        }

        public async ValueTask Subscribe(OnSubscribed observe, OnDropped release, CancellationToken cancel)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel, Halt.Token);

            _observed = observe;
            _released = release;
            await Subscribe(cts.Token).ConfigureAwait(false);
            Started = true;
            _observed(_sub.SubscriptionId);
        }
        protected abstract ValueTask Subscribe(CancellationToken cancellationToken);
    }
}
