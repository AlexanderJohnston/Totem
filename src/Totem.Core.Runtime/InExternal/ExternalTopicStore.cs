using System.Collections.Concurrent;
using Totem.Core;
using Totem.InExternal.Services;
using Totem.InMemory.Events;
using Totem.Topics;

namespace Totem.InExternal;

public class ExternalTopicStore : ITopicStore
{
    readonly ConcurrentDictionary<ITopicTransaction, ExternalTopicStream> _streamsByTransaction = new();
    readonly IAbstractEventStoreService _eventStoreService;
    readonly IServiceProvider _services;
    readonly IClock _clock;
    readonly IInMemoryEventBus _eventBus;
    readonly RuntimeMap _map;
    readonly TotemJsonFormat _jsonFormat;

    public ExternalTopicStore(
        IAbstractEventStoreService eventStoreService,
        IServiceProvider services,
        IClock clock,
        IInMemoryEventBus eventBus,
        RuntimeMap map)
    {
        _eventStoreService = eventStoreService;
        _services = services;
        _clock = clock;
        _eventBus = eventBus;
        _map = map;
        _jsonFormat = new TotemJsonFormat();
    }

    public async Task<ITopicTransaction> StartTransactionAsync(ITopicContext<ICommand> context, CancellationToken cancellationToken)
    {
        var topic = (ITopic) _services.GetRequiredService(context.TopicKey.DeclaredType);

        if(topic is ITimelineInit init)
        {
            init.TimelineId = context.TopicId;
        }

        var stream = new ExternalTopicStream(context.TopicType, _clock, _map, _eventStoreService, _jsonFormat);
        var version = await stream.LoadAsync(topic, context, cancellationToken);
        var transaction = new TopicTransaction(this, context, topic, version, cancellationToken);

        if(!_streamsByTransaction.TryAdd(transaction, stream))
        {
            throw new InvalidOperationException("Failed to register external topic stream for transaction.");
        }

        return transaction;
    }

    public async Task CommitAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        if(!_streamsByTransaction.TryRemove(transaction, out var stream))
        {
            throw new InvalidOperationException("Missing external topic stream for transaction.");
        }

        var newEvents = await stream.CommitAsync(transaction, cancellationToken);

        if(newEvents.Count > 0)
        {
            _eventBus.Publish(newEvents);
        }
    }

    public Task RollbackAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        _streamsByTransaction.TryRemove(transaction, out _);
        return Task.CompletedTask;
    }
}
