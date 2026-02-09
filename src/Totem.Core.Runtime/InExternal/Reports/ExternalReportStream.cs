using System.Text;
using System.Text.Json;
using EventStore.Client;
using Totem;
using Totem.Core;
using Totem.Events;
using Totem.InExternal.Services;
using Totem.InMemory;
using Totem.Map;
using Totem.Reports;

namespace Totem.InExternal.Reports;

internal sealed class ExternalReportStream
{
    const int ReadBatchSize = 512;
    const string EventTypeName = "report-row";

    readonly ReportType _reportType;
    readonly TimelineKey _reportKey;
    readonly IAbstractEventStoreService _eventStoreService;
    readonly TotemJsonFormat _jsonFormat;
    readonly RuntimeMap _map;
    TimelinePosition _position = TimelinePosition.Start;
    string? _rowJson;
    DateTimeOffset _whenUpdated;
    bool _hasSnapshot;

    internal ExternalReportStream(
        ReportType reportType,
        TimelineKey reportKey,
        IAbstractEventStoreService eventStoreService,
        TotemJsonFormat jsonFormat,
        RuntimeMap map)
    {
        _reportType = reportType;
        _reportKey = reportKey;
        _eventStoreService = eventStoreService;
        _jsonFormat = jsonFormat;
        _map = map;
    }

    internal async Task<TimelinePosition> LoadAsync(IReport report, CancellationToken cancellationToken)
    {
        _position = TimelinePosition.Start;
        _hasSnapshot = false;
        _rowJson = null;

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
                var metadata = DeserializeMetadata(resolvedEvent.Event.Metadata.Span);

                if(metadata is null)
                {
                    continue;
                }

                var envelope = DeserializeEnvelope(metadata);

                if(envelope is null)
                {
                    continue;
                }

                var eventContext = _map.CreateContext(envelope);

                _reportType.CallGivenIfDefined(report, eventContext);

                _position = _position.Next();
                _rowJson = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
                _whenUpdated = metadata.WhenOccurred;
                _hasSnapshot = true;
            }

            if(batch.Events.Count < ReadBatchSize)
            {
                break;
            }

            var lastEventNumber = batch.Events[^1].Event.EventNumber;
            start = new StreamPosition(lastEventNumber + 1);
        }

        return _position;
    }

    internal async Task<ReportCommitResult> CommitAsync(IReportTransaction transaction, CancellationToken cancellationToken)
    {
        transaction.Context.ReportKey.CheckConcurrency(_position, transaction.Position);

        var envelope = transaction.Context.Envelope;
        var eventType = envelope.EventType;

        var rowType = _reportType.Row.DeclaredType;
        var rowJson = JsonSerializer.Serialize(transaction.Report.Row, rowType, _jsonFormat.Options);
        var data = JsonSerializer.SerializeToUtf8Bytes(transaction.Report.Row, rowType, _jsonFormat.Options);

        var metadata = new ExternalReportRowMetadata
        {
            EventClrType = eventType.AssemblyQualifiedName ?? eventType.FullName ?? eventType.Name,
            EventJson = JsonSerializer.Serialize(envelope.Event, eventType, _jsonFormat.Options),
            EventId = envelope.EventId.ToString(),
            CorrelationId = envelope.Info.CorrelationId.ToString(),
            TopicClrType = envelope.TopicKey.DeclaredType.AssemblyQualifiedName ?? envelope.TopicKey.DeclaredType.FullName ?? envelope.TopicKey.DeclaredType.Name,
            TopicId = envelope.TopicKey.Id.ToString(),
            RowClrType = rowType.AssemblyQualifiedName ?? rowType.FullName ?? rowType.Name,
            WhenOccurred = envelope.WhenOccurred
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, _jsonFormat.Options);
        var eventData = new EventData(Uuid.NewUuid(), EventTypeName, data, metadataBytes);
        var expectedRevision = GetExpectedRevision(transaction.Position);

        await _eventStoreService.AppendToStreamAsync(GetStreamName(), expectedRevision, eventData, cancellationToken);

        var wasNew = !_hasSnapshot;

        _position = _position.Next();
        _rowJson = rowJson;
        _whenUpdated = metadata.WhenOccurred;
        _hasSnapshot = true;

        return new ReportCommitResult(_position, wasNew, _whenUpdated);
    }

    internal async Task<ReportSnapshot?> ReadSnapshotAsync(CancellationToken cancellationToken)
    {
        if(_hasSnapshot && _rowJson is not null)
        {
            return new ReportSnapshot(_position, _whenUpdated, _rowJson);
        }

        var result = await _eventStoreService.ReadStreamEventsBackwardsAsync(GetStreamName(), 1, cancellationToken);

        if(result.Events.Count == 0)
        {
            return null;
        }

        var resolved = result.Events[0];
        var metadata = DeserializeMetadata(resolved.Event.Metadata.Span);

        if(metadata is null)
        {
            return null;
        }

        var rowJson = Encoding.UTF8.GetString(resolved.Event.Data.Span);
        var eventNumber = resolved.Event.EventNumber.ToUInt64();
        var position = TimelinePosition.From((long) eventNumber);

        _position = position;
        _rowJson = rowJson;
        _whenUpdated = metadata.WhenOccurred;
        _hasSnapshot = true;

        return new ReportSnapshot(position, metadata.WhenOccurred, rowJson);
    }

    internal IReportRow? DeserializeRow(string? rowJson)
    {
        if(string.IsNullOrWhiteSpace(rowJson))
        {
            return null;
        }

        var row = JsonSerializer.Deserialize(rowJson, _reportType.Row.DeclaredType, _jsonFormat.Options);

        return row as IReportRow;
    }

    string GetStreamName() => _reportKey.ToString();

    static StreamRevision GetExpectedRevision(TimelinePosition position) =>
        position.IsStart ? StreamRevision.None : new StreamRevision((ulong) position.ToIndex());

    ExternalReportRowMetadata? DeserializeMetadata(ReadOnlySpan<byte> metadata)
    {
        if(metadata.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ExternalReportRowMetadata>(metadata, _jsonFormat.Options);
        }
        catch(JsonException)
        {
            return null;
        }
    }

    EventEnvelope? DeserializeEnvelope(ExternalReportRowMetadata metadata)
    {
        var eventType = ExternalTypeResolver.Resolve(metadata.EventClrType);
        var topicType = ExternalTypeResolver.Resolve(metadata.TopicClrType);

        if(eventType is null || topicType is null)
        {
            return null;
        }

        var e = (IEvent?) JsonSerializer.Deserialize(metadata.EventJson, eventType, _jsonFormat.Options);

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

    sealed class ExternalReportRowMetadata
    {
        public string EventClrType { get; set; } = string.Empty;
        public string EventJson { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string TopicClrType { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string RowClrType { get; set; } = string.Empty;
        public DateTimeOffset WhenOccurred { get; set; }
    }

    internal readonly struct ReportSnapshot
    {
        public ReportSnapshot(TimelinePosition position, DateTimeOffset whenUpdated, string rowJson)
        {
            Position = position;
            WhenUpdated = whenUpdated;
            RowJson = rowJson;
        }

        public TimelinePosition Position { get; }
        public DateTimeOffset WhenUpdated { get; }
        public string RowJson { get; }
    }

    internal readonly struct ReportCommitResult
    {
        public ReportCommitResult(TimelinePosition position, bool isNewRow, DateTimeOffset whenUpdated)
        {
            Position = position;
            IsNewRow = isNewRow;
            WhenUpdated = whenUpdated;
        }

        public TimelinePosition Position { get; }
        public bool IsNewRow { get; }
        public DateTimeOffset WhenUpdated { get; }
    }

}
