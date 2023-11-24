namespace Totem.InMemory;

internal sealed class InMemoryTopicStream
{
    readonly List<IEventContext<IEvent>> _events = new();
    readonly TopicType _type;
    readonly IClock _clock;
    readonly RuntimeMap _map;
    TimelinePosition _version;

    internal InMemoryTopicStream(TopicType type, IClock clock, RuntimeMap map)
    {
        _type = type;
        _clock = clock;
        _map = map;
    }

    internal TimelinePosition Load(ITopic topic)
    {
        foreach(var e in _events)
        {
            _type.CallGivenIfDefined(topic, e);

            _version = _version.Next();
        }

        return _version;
    }

    internal IReadOnlyList<EventEnvelope> Commit(ITopicTransaction transaction)
    {
        var topicKey = transaction.Context.TopicKey;
        var correlationId = transaction.Context.CorrelationId;
        var principal = transaction.Context.Principal;
        var now = _clock.UtcNow;
        var envelopes = new List<EventEnvelope>();

        topicKey.CheckConcurrency(_version, transaction.Position);

        foreach(var newEvent in transaction.Topic.NewEvents)
        {
            var envelope = new EventEnvelope(newEvent, topicKey, now, correlationId, principal);

            _events.Add(_map.CreateContext(envelope));

            _version = _version.Next();

            envelopes.Add(envelope);
        }

        return envelopes;
    }
}
