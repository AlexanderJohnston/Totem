using System;
using System.Linq;
using System.Text.Json;
using EventStore.Client;
using Totem;
using Totem.Core;
using Totem.Events;
using Totem.InExternal.Services;
using Totem.InMemory;
using Totem.Topics;

namespace Totem.InExternal;

internal sealed class ExternalTopicStream
{
    const int ReadBatchSize = 512;

    readonly TopicType _type;
    readonly IClock _clock;
    readonly RuntimeMap _map;
    readonly IAbstractEventStoreService _eventStoreService;
    readonly TotemJsonFormat _jsonFormat;
    TimelinePosition _version = TimelinePosition.Start;

    internal ExternalTopicStream(TopicType type, IClock clock, RuntimeMap map, IAbstractEventStoreService eventStoreService, TotemJsonFormat jsonFormat)
    {
        _type = type;
        _clock = clock;
        _map = map;
        _eventStoreService = eventStoreService;
        _jsonFormat = jsonFormat;
    }

    internal async Task<TimelinePosition> LoadAsync(ITopic topic, ITopicContext<ICommand> context, CancellationToken cancellationToken)
    {
        _version = TimelinePosition.Start;

        var streamName = context.TopicKey.ToString();
        var start = StreamPosition.Start;

        while(true)
        {
            var result = await _eventStoreService.ReadStreamEventsAsync(streamName, start, ReadBatchSize, cancellationToken);

            if(result.Events.Count == 0)
            {
                break;
            }

            ulong? lastEventNumber = null;

            foreach(var resolvedEvent in result.Events)
            {
                lastEventNumber = resolvedEvent.Event.EventNumber;

                var envelope = DeserializeEnvelope(resolvedEvent, context.TopicKey);

                if(envelope is null)
                {
                    continue;
                }

                var eventContext = _map.CreateContext(envelope);

                _type.CallGivenIfDefined(topic, eventContext);

                _version = _version.Next();
            }

            if(lastEventNumber.HasValue)
            {
                start = new StreamPosition(lastEventNumber.Value + 1);
            }

            if(result.Events.Count < ReadBatchSize)
            {
                break;
            }
        }

        return _version;
    }

    internal async Task<IReadOnlyList<EventEnvelope>> CommitAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        var topicKey = transaction.Context.TopicKey;
        var correlationId = transaction.Context.CorrelationId;
        var principal = transaction.Context.Principal;
        var now = _clock.UtcNow;
        var pendingEvents = transaction.Topic.NewEvents;

        if(pendingEvents.Count == 0)
        {
            return Array.Empty<EventEnvelope>();
        }

        topicKey.CheckConcurrency(_version, transaction.Position);

        var envelopes = new List<EventEnvelope>(pendingEvents.Count);
        var eventData = new List<EventData>(pendingEvents.Count);

        foreach(var newEvent in pendingEvents)
        {
            var envelope = new EventEnvelope(newEvent, topicKey, now, correlationId, principal);
            envelopes.Add(envelope);

            var metadata = new ExternalEventMetadata
            {
                EventClrType = newEvent.GetType().AssemblyQualifiedName,
                CorrelationId = correlationId?.ToString(),
                WhenOccurred = now
            };

            eventData.Add(CreateEventData(newEvent, metadata));
        }

        var expectedRevision = GetExpectedRevision(transaction.Position);

        await _eventStoreService.AppendToStreamAsync(topicKey.ToString(), expectedRevision, eventData, cancellationToken);

        foreach(var _ in envelopes)
        {
            _version = _version.Next();
        }

        return envelopes;
    }

    EventEnvelope? DeserializeEnvelope(ResolvedEvent resolvedEvent, TimelineKey topicKey)
    {
        var metadata = DeserializeMetadata(resolvedEvent.Event.Metadata.Span);
        var eventTypeName = metadata?.EventClrType ?? resolvedEvent.Event.EventType;

        if(string.IsNullOrWhiteSpace(eventTypeName))
        {
            return null;
        }

        Type? eventType = TryGetEventType(eventTypeName);

        if(eventType is null)
        {
            return null;
        }

        var instance = (IEvent?) JsonSerializer.Deserialize(resolvedEvent.Event.Data.Span, eventType, _jsonFormat.Options);

        if(instance is null)
        {
            return null;
        }

        var when = metadata?.WhenOccurred ?? _clock.UtcNow;
        var correlationId = ConvertToId(metadata?.CorrelationId);

        return new EventEnvelope(instance, topicKey, when, correlationId, principal: null);
    }

    Type? TryGetEventType(string eventTypeName)
    {
        foreach(var messageType in _type.Givens.MessageTypes)
        {
            var declaredType = messageType.DeclaredType;

            if(string.Equals(declaredType.FullName, eventTypeName, StringComparison.Ordinal)
                || string.Equals(declaredType.Name, eventTypeName, StringComparison.Ordinal))
            {
                return declaredType;
            }
        }

        var resolved = Type.GetType(eventTypeName, throwOnError: false);

        if(resolved is not null)
        {
            return resolved;
        }

        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(eventTypeName, throwOnError: false, ignoreCase: false);

            if(type is not null)
            {
                return type;
            }
        }

        return null;
    }

    ExternalEventMetadata? DeserializeMetadata(ReadOnlySpan<byte> metadata)
    {
        if(metadata.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExternalEventMetadata>(metadata, _jsonFormat.Options);
        }
        catch
        {
            return null;
        }
    }

    EventData CreateEventData(IEvent @event, ExternalEventMetadata metadata)
    {
        var eventType = @event.GetType();
        var data = JsonSerializer.SerializeToUtf8Bytes(@event, eventType, _jsonFormat.Options);
        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, _jsonFormat.Options);
        var typeName = eventType.AssemblyQualifiedName ?? eventType.FullName ?? eventType.Name;

        return new EventData(Uuid.NewUuid(), typeName, data, metadataBytes);
    }

    static Id? ConvertToId(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : (Id) value;

    static StreamRevision GetExpectedRevision(TimelinePosition position) =>
        position.IsStart ? StreamRevision.None : new StreamRevision((ulong) position.ToIndex());

    sealed class ExternalEventMetadata
    {
        public string? EventClrType { get; set; }
        public string? CorrelationId { get; set; }
        public DateTimeOffset WhenOccurred { get; set; }
    }
}
