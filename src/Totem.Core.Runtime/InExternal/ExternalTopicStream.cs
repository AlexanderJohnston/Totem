using System.Collections.Generic;
using EventStore.Client;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;
using Totem;
using System.Text;
using Totem.InExternal.Services;
using Totem.InMemory;

internal sealed class ExternalTopicStream
{
    readonly TopicType _type;
    readonly IClock _clock;
    readonly RuntimeMap _map;
    readonly IAbstractEventStoreService _eventStoreService;
    readonly TotemJsonFormat _jsonFormat;
    TimelinePosition _version;

    internal ExternalTopicStream(TopicType type, IClock clock, RuntimeMap map, IAbstractEventStoreService eventStoreService, TotemJsonFormat jsonFormat)
    {
        _type = type;
        _clock = clock;
        _map = map;
        _eventStoreService = eventStoreService;
        _jsonFormat = jsonFormat;
    }

    internal async Task<TimelinePosition> LoadAsync(ITopic topic, string streamName)
    {
        var result = await _eventStoreService.ReadStreamEventsAsync(streamName, StreamPosition.Start, 100, CancellationToken.None);

        foreach(var resolvedEvent in result.Events)
        {
            var e = JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, _type.GetEventType(resolvedEvent.Event.EventType));

            if(e is IEventContext<IEvent> eventContext)
            {
                _type.CallGivenIfDefined(topic, eventContext);

                _version = _version.Next();
            }
        }

        return _version;
    }

    internal async Task<IReadOnlyList<EventEnvelope>> CommitAsync(ITopicTransaction transaction)
    {
        var topicKey = transaction.Context.TopicKey;
        var correlationId = transaction.Context.CorrelationId;
        var principal = transaction.Context.Principal;
        var now = _clock.UtcNow;
        var envelopes = new List<EventEnvelope>();

        topicKey.CheckConcurrency(_version, transaction.Position);

        var newEvents = transaction.UncommittedEvents.Select(x => new EventData(Uuid.NewUuid(), x.GetType().FullName, Encoding.UTF8.GetBytes(x.ToJson())));


        List<EventEnvelope> events = new List<EventEnvelope>();
        foreach(var newEvent in transaction.Topic.NewEvents)
        {
            var eventJson = JsonSerializer.Serialize(newEvent, _jsonFormat.Options);
            var eventData = new EventData(Uuid.NewUuid(), newEvent.GetType().FullName, Encoding.UTF8.GetBytes(eventJson));
            await _eventStoreService.AppendToStreamAsync(transaction.Context.TopicKey.ToString(), _version, eventData, CancellationToken.None);
            var envelope = new EventEnvelope(newEvent, topicKey, now, correlationId, principal);
            _version = _version.Next();
            events.Add(envelope);
        }

        return events;
    }
}
