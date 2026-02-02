using System.Text.Json;
using EventStore.Client;
using Totem.Core;
using Totem.Events;
using Totem.InExternal.Services;
using Totem.InMemory;
using Totem.Map;
using Totem.Workflows;

namespace Totem.InExternal;

internal sealed class ExternalWorkflowStream
{
    const int ReadBatchSize = 512;
    const string EventTypeName = "workflow-event";

    readonly WorkflowType _type;
    readonly TimelineKey _workflowKey;
    readonly IAbstractEventStoreService _eventStoreService;
    readonly TotemJsonFormat _jsonFormat;
    readonly RuntimeMap _map;
    TimelinePosition _version = TimelinePosition.Start;

    internal ExternalWorkflowStream(
        WorkflowType type,
        TimelineKey workflowKey,
        IAbstractEventStoreService eventStoreService,
        TotemJsonFormat jsonFormat,
        RuntimeMap map)
    {
        _type = type;
        _workflowKey = workflowKey;
        _eventStoreService = eventStoreService;
        _jsonFormat = jsonFormat;
        _map = map;
    }

    internal async Task<TimelinePosition> LoadAsync(IWorkflow workflow, IWorkflowContext<IEvent> context, CancellationToken cancellationToken)
    {
        _version = TimelinePosition.Start;

        var streamName = GetStreamName();
        var start = StreamPosition.Start;

        while(true)
        {
            var batch = await _eventStoreService.ReadStreamEventsAsync(streamName, start, ReadBatchSize, cancellationToken);

            if(batch.Events.Count == 0)
            {
                break;
            }

            foreach(var resolvedEvent in batch.Events)
            {
                var envelope = DeserializeEnvelope(resolvedEvent.Event.Data.Span, resolvedEvent.Event.Metadata.Span);

                if(envelope is null)
                {
                    continue;
                }

                var eventContext = _map.CreateContext(envelope);

                _type.CallGivenIfDefined(workflow, eventContext);

                _version = _version.Next();
            }

            if(batch.Events.Count < ReadBatchSize)
            {
                break;
            }

            var lastEventNumber = batch.Events[^1].Event.EventNumber;
            start = new StreamPosition(lastEventNumber + 1);
        }

        return _version;
    }

    internal async Task<IReadOnlyList<WorkflowCommandEnvelope>> CommitAsync(IWorkflowTransaction transaction, CancellationToken cancellationToken)
    {
        transaction.Context.WorkflowKey.CheckConcurrency(_version, transaction.Position);

        var envelope = transaction.Context.Envelope;
        var eventType = envelope.EventType;
        var data = JsonSerializer.SerializeToUtf8Bytes(envelope.Event, eventType, _jsonFormat.Options);

        var metadata = new ExternalWorkflowEventMetadata
        {
            EventClrType = eventType.AssemblyQualifiedName ?? eventType.FullName ?? eventType.Name,
            EventId = envelope.EventId.ToString(),
            CorrelationId = envelope.Info.CorrelationId.ToString(),
            WhenOccurred = envelope.WhenOccurred,
            TopicClrType = envelope.TopicKey.DeclaredType.AssemblyQualifiedName ?? envelope.TopicKey.DeclaredType.FullName ?? envelope.TopicKey.DeclaredType.Name,
            TopicId = envelope.TopicKey.Id.ToString()
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, _jsonFormat.Options);
        var eventData = new EventData(Uuid.NewUuid(), EventTypeName, data, metadataBytes);
        var expectedRevision = GetExpectedRevision(transaction.Position);

        await _eventStoreService.AppendToStreamAsync(GetStreamName(), expectedRevision, eventData, cancellationToken);

        _version = _version.Next();

        return transaction.Workflow.GetNewCommands();
    }

    string GetStreamName() => _workflowKey.ToString();

    static StreamRevision GetExpectedRevision(TimelinePosition position) =>
        position.IsStart ? StreamRevision.None : new StreamRevision((ulong) position.ToIndex());

    ExternalWorkflowEventMetadata? DeserializeMetadata(ReadOnlySpan<byte> metadata)
    {
        if(metadata.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExternalWorkflowEventMetadata>(metadata, _jsonFormat.Options);
        }
        catch(JsonException)
        {
            return null;
        }
    }

    EventEnvelope? DeserializeEnvelope(ReadOnlySpan<byte> data, ReadOnlySpan<byte> metadataBytes)
    {
        var metadata = DeserializeMetadata(metadataBytes);

        if(metadata is null)
        {
            return null;
        }

        var eventType = ExternalTypeResolver.Resolve(metadata.EventClrType);
        var topicType = ExternalTypeResolver.Resolve(metadata.TopicClrType);

        if(eventType is null || topicType is null)
        {
            return null;
        }

        var e = (IEvent?) JsonSerializer.Deserialize(data, eventType, _jsonFormat.Options);

        if(e is null)
        {
            return null;
        }

        if(!Id.TryFrom(metadata.TopicId, out var topicId))
        {
            return null;
        }

        var topicKey = new TimelineKey(topicType, topicId);
        var messageId = Id.TryFrom(metadata.EventId, out var eventId) ? eventId : Id.NewId();
        var correlationId = Id.TryFrom(metadata.CorrelationId, out var correlation) ? correlation : Id.NewId();
        var info = new EnvelopeInfo(messageId, correlationId);

        return new EventEnvelope(e, topicKey, metadata.WhenOccurred, info);
    }

    sealed class ExternalWorkflowEventMetadata
    {
        public string EventClrType { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public DateTimeOffset WhenOccurred { get; set; }
        public string TopicClrType { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
    }
}
