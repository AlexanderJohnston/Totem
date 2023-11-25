//using EventStore.Client;
//using System.Text;
//using Totem.InExternal.Services;
//using Totem.InMemory.Events;

//namespace Totem.InExternal
//{
//    public class ExternalTopicStore : ITopicStore
//    {
//        private readonly IAbstractEventStoreService _eventStoreService;
//        private readonly IServiceProvider _services;
//        private readonly IClock _clock;
//        private readonly IInMemoryEventBus _eventBus;
//        private readonly RuntimeMap _map;

//        public ExternalTopicStore(IAbstractEventStoreService eventStoreService, IServiceProvider services, IClock clock, IInMemoryEventBus eventBus, RuntimeMap map)
//        {
//            _eventStoreService = eventStoreService;
//            _services = services;
//            _clock = clock;
//            _eventBus = eventBus;
//            _map = map;
//        }

//        public async Task<ITopicTransaction> StartTransactionAsync(ITopicContext<ICommand> context, CancellationToken cancellationToken)
//        {
//            var topic = (ITopic) _services.GetRequiredService(context.TopicKey.DeclaredType);

//            if(topic is ITimelineInit init)
//            {
//                init.TimelineId = context.TopicId;
//            }

//            var version = await _eventStoreService.StreamExistsAsync(context.TopicKey.ToString(), cancellationToken)
//                ? TimelinePosition.Start
//                : TimelinePosition.Start;

//            var transaction = new TopicTransaction(this, context, topic, version, cancellationToken);

//            return transaction;
//        }

//        public async Task CommitAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
//        {
//            var newEvents = transaction.UncommittedEvents.Select(x => new EventData(Uuid.NewUuid(), x.GetType().FullName, Encoding.UTF8.GetBytes(x.ToJson())));
//            var appendResult = await _eventStoreService.AppendToStreamAsync(transaction.Context.TopicKey.ToString(), StreamRevision.Any, newEvents, cancellationToken);

//            _eventBus.Publish(transaction.UncommittedEvents);
//        }

//        public Task RollbackAsync(ITopicTransaction transaction, CancellationToken cancellationToken) =>
//            // External event store may not keep state for open transactions
//            Task.CompletedTask;
//    }
//}
