using Totem.InMemory.Events;

namespace Totem.InMemory;

public sealed class InMemoryTopicStore : ITopicStore
{
    readonly ConcurrentDictionary<TimelineKey, InMemoryTopicStream> _streamsByKey = new();
    readonly IServiceProvider _services;
    readonly IClock _clock;
    readonly IInMemoryEventBus _eventBus;
    readonly RuntimeMap _map;

    public InMemoryTopicStore(IServiceProvider services, IClock clock, IInMemoryEventBus eventBus, RuntimeMap map)
    {
        _services = services;
        _clock = clock;
        _eventBus = eventBus;
        _map = map;
    }

    public Task<ITopicTransaction> StartTransactionAsync(ITopicContext<ICommand> context, CancellationToken cancellationToken)
    {
        var topic = (ITopic) _services.GetRequiredService(context.TopicKey.DeclaredType);

        if(topic is ITimelineInit init)
        {
            init.TimelineId = context.TopicId;
        }

        var version = _streamsByKey.TryGetValue(context.TopicKey, out var stream)
            ? stream.Load(topic)
            : TimelinePosition.Start;

        var transaction = new TopicTransaction(this, context, topic, version, cancellationToken);

        return Task.FromResult<ITopicTransaction>(transaction);
    }

    public Task CommitAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        _streamsByKey.AddOrUpdate(
            transaction.Context.TopicKey,
            _ => AddStream(transaction),
            (_, stream) => UpdateStream(stream, transaction));

        return Task.CompletedTask;
    }

    public Task RollbackAsync(ITopicTransaction transaction, CancellationToken cancellationToken) =>
        // In-memory store does not keep state for open transactions
        Task.CompletedTask;

    InMemoryTopicStream AddStream(ITopicTransaction transaction)
    {
        var stream = new InMemoryTopicStream(transaction.Context.TopicType, _clock, _map);

        return UpdateStream(stream, transaction);
    }

    InMemoryTopicStream UpdateStream(InMemoryTopicStream stream, ITopicTransaction transaction)
    {
        var newEvents = stream.Commit(transaction);

        _eventBus.Publish(newEvents);

        return stream;
    }
}
