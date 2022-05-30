using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Totem.Core;
using Totem.Map;
using Totem.Topics;

namespace Totem.External;
public class ExternalTopicStore : ITopicStore
{
    readonly ConcurrentDictionary<ItemKey, ExternalTopicHistory> _historiesByKey = new();
    readonly IServiceProvider _services;
    readonly IExternalEventSubscription _subscription;
    readonly IClock _clock;

    public ExternalTopicStore(IServiceProvider services, IExternalEventSubscription subscription, IClock clock)
    {
        _services = services;
        _subscription = subscription;
        _clock = clock;
    }

    public Task CommitAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is null)
            throw new ArgumentNullException(nameof(transaction));

        _historiesByKey.AddOrUpdate(
            transaction.Context.TopicKey,
            _ => AddHistory(transaction),
            (_, history) => UpdateHistory(history, transaction));

        return Task.CompletedTask;
    }

    public Task RollbackAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        // Nothing to do when in memory
        return Task.CompletedTask;
    }

    public Task<ITopicTransaction> StartTransactionAsync(ITopicContext<ICommandMessage> context, CancellationToken cancellationToken)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var topic = (ITopic)_services.GetRequiredService(context.TopicKey.DeclaredType);

        topic.Id = context.TopicKey.Id;

        if (_historiesByKey.TryGetValue(context.TopicKey, out var history))
        {
            history.Load(topic);
        }

        return Task.FromResult<ITopicTransaction>(new TopicTransaction(this, context, topic, cancellationToken));
    }

    ExternalTopicHistory AddHistory(ITopicTransaction transaction)
    {
        var history = new ExternalTopicHistory(transaction);

        foreach (var newPoint in history.Commit(transaction, _clock.UtcNow))
        {
            _subscription.Publish(newPoint);
        }

        return history;
    }

    ExternalTopicHistory UpdateHistory(ExternalTopicHistory history, ITopicTransaction transaction)
    {
        foreach (var newPoint in history.Commit(transaction, _clock.UtcNow))
        {
            _subscription.Publish(newPoint);
        }

        return history;
    }

}
class ExternalTopicHistory
{
    readonly List<IEvent> _events = new();
    readonly TopicType _topicType;

    internal ExternalTopicHistory(ITopicTransaction transaction) =>
        _topicType = transaction.Context.TopicType;

    internal long Version => _events.Count - 1;

    internal void Load(ITopic topic)
    {
        foreach (var e in _events)
        {
            _topicType.CallGivenIfDefined(topic, e);
        }
    }

    internal IEnumerable<IEventEnvelope> Commit(ITopicTransaction transaction, DateTimeOffset now)
    {
        foreach (var newEvent in transaction.Topic.TakeNewEvents())
        {
            var type = newEvent.GetType();
            var envelope = new EventEnvelope(
                new ItemKey(type),
                newEvent,
                EventInfo.From(type),   
                transaction.Context.CommandContext.CorrelationId,
                transaction.Context.CommandContext.Principal,
                now);

            _events.Add(envelope.Message);

            yield return envelope;
        }
    }
}
