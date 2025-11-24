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
                var record = DeserializeRecord(resolvedEvent.Event.Data.Span);

                if(record is null)
                {
                    continue;
                }

                var envelope = CreateEnvelope(record);

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

        var record = WorkflowEventRecord.From(transaction.Context.Envelope, _jsonFormat);
        var eventData = new EventData(Uuid.NewUuid(), EventTypeName, JsonSerializer.SerializeToUtf8Bytes(record, _jsonFormat.Options));
        var expectedRevision = GetExpectedRevision(transaction.Position);

        await _eventStoreService.AppendToStreamAsync(GetStreamName(), expectedRevision, eventData, cancellationToken);

        _version = _version.Next();

        return transaction.Workflow.GetNewCommands();
    }

    string GetStreamName() => _workflowKey.ToString();

    static StreamRevision GetExpectedRevision(TimelinePosition position) =>
        position.IsStart ? StreamRevision.None : new StreamRevision((ulong) position.ToIndex());

    WorkflowEventRecord? DeserializeRecord(ReadOnlySpan<byte> data)
    {
        if(data.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WorkflowEventRecord>(data, _jsonFormat.Options);
        }
        catch(JsonException)
        {
            return null;
        }
    }

    EventEnvelope? CreateEnvelope(WorkflowEventRecord record)
    {
        var eventType = ExternalTypeResolver.Resolve(record.EventClrType);
        var topicType = ExternalTypeResolver.Resolve(record.TopicClrType);

        if(eventType is null || topicType is null)
        {
            return null;
        }

        var e = (IEvent?) JsonSerializer.Deserialize(record.EventJson, eventType, _jsonFormat.Options);

        if(e is null)
        {
            return null;
        }

        if(!Id.TryFrom(record.TopicId, out var topicId))
        {
            return null;
        }

        var topicKey = new TimelineKey(topicType, topicId);
        var messageId = Id.TryFrom(record.EventId, out var eventId) ? eventId : Id.NewId();
        var correlationId = Id.TryFrom(record.CorrelationId, out var correlation) ? correlation : Id.NewId();
        var info = new EnvelopeInfo(messageId, correlationId);

        return new EventEnvelope(e, topicKey, record.WhenOccurred, info);
    }

    sealed class WorkflowEventRecord
    {
        public string EventClrType { get; set; } = string.Empty;
        public string EventJson { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public DateTimeOffset WhenOccurred { get; set; }
        public string TopicClrType { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;

        public static WorkflowEventRecord From(EventEnvelope envelope, TotemJsonFormat jsonFormat)
        {
            return new WorkflowEventRecord
            {
                EventClrType = envelope.EventType.AssemblyQualifiedName ?? envelope.EventType.FullName ?? envelope.EventType.Name,
                EventJson = JsonSerializer.Serialize(envelope.Event, envelope.EventType, jsonFormat.Options),
                EventId = envelope.EventId.ToString(),
                CorrelationId = envelope.Info.CorrelationId.ToString(),
                WhenOccurred = envelope.WhenOccurred,
                TopicClrType = envelope.TopicKey.DeclaredType.AssemblyQualifiedName ?? envelope.TopicKey.DeclaredType.FullName ?? envelope.TopicKey.DeclaredType.Name,
                TopicId = envelope.TopicKey.Id.ToString()
            };
        }
    }
}
