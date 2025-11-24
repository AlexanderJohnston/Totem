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

                _reportType.CallGivenIfDefined(report, eventContext);

                _position = _position.Next();
                _rowJson = record.RowJson;
                _whenUpdated = record.WhenOccurred;
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

        var record = ReportRowRecord.From(transaction.Context.Envelope, transaction.Report.Row, _reportType, _jsonFormat);
        var eventData = new EventData(Uuid.NewUuid(), EventTypeName, JsonSerializer.SerializeToUtf8Bytes(record, _jsonFormat.Options));
        var expectedRevision = GetExpectedRevision(transaction.Position);

        await _eventStoreService.AppendToStreamAsync(GetStreamName(), expectedRevision, eventData, cancellationToken);

        var wasNew = !_hasSnapshot;

        _position = _position.Next();
        _rowJson = record.RowJson;
        _whenUpdated = record.WhenOccurred;
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
        var record = DeserializeRecord(resolved.Event.Data.Span);

        if(record is null)
        {
            return null;
        }

        var eventNumber = resolved.Event.EventNumber.ToUInt64();
        var position = TimelinePosition.From((long) eventNumber);

        _position = position;
        _rowJson = record.RowJson;
        _whenUpdated = record.WhenOccurred;
        _hasSnapshot = true;

        return new ReportSnapshot(position, record.WhenOccurred, record.RowJson);
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

    ReportRowRecord? DeserializeRecord(ReadOnlySpan<byte> data)
    {
        if(data.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReportRowRecord>(data, _jsonFormat.Options);
        }
        catch(JsonException)
        {
            return null;
        }
    }

    EventEnvelope? CreateEnvelope(ReportRowRecord record)
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

    sealed class ReportRowRecord
    {
        public string EventClrType { get; set; } = string.Empty;
        public string EventJson { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string TopicClrType { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string RowClrType { get; set; } = string.Empty;
        public string RowJson { get; set; } = string.Empty;
        public DateTimeOffset WhenOccurred { get; set; }

        public static ReportRowRecord From(EventEnvelope envelope, IReportRow row, ReportType reportType, TotemJsonFormat jsonFormat)
        {
            var rowType = reportType.Row.DeclaredType;

            return new ReportRowRecord
            {
                EventClrType = envelope.EventType.AssemblyQualifiedName ?? envelope.EventType.FullName ?? envelope.EventType.Name,
                EventJson = JsonSerializer.Serialize(envelope.Event, envelope.EventType, jsonFormat.Options),
                EventId = envelope.EventId.ToString(),
                CorrelationId = envelope.Info.CorrelationId.ToString(),
                TopicClrType = envelope.TopicKey.DeclaredType.AssemblyQualifiedName ?? envelope.TopicKey.DeclaredType.FullName ?? envelope.TopicKey.DeclaredType.Name,
                TopicId = envelope.TopicKey.Id.ToString(),
                RowClrType = rowType.AssemblyQualifiedName ?? rowType.FullName ?? rowType.Name,
                RowJson = JsonSerializer.Serialize(row, rowType, jsonFormat.Options),
                WhenOccurred = envelope.WhenOccurred
            };
        }
    }
}
