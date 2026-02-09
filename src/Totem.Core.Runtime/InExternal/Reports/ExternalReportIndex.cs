using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Totem;
using Totem.InExternal.Services;
using Totem.Reports;

namespace Totem.InExternal.Reports;

internal sealed class ExternalReportIndex
{
    const int ReadBatchSize = 512;
    const string EventTypeName = "report-index";

    readonly ReportType _reportType;
    readonly IAbstractEventStoreService _eventStoreService;
    readonly TotemJsonFormat _jsonFormat;
    readonly ConcurrentDictionary<Id, byte> _rowIds = new();
    readonly SemaphoreSlim _gate = new(1, 1);
    bool _isLoaded;
    StreamRevision? _expectedRevision;

    internal ExternalReportIndex(ReportType reportType, IAbstractEventStoreService eventStoreService, TotemJsonFormat jsonFormat)
    {
        _reportType = reportType;
        _eventStoreService = eventStoreService;
        _jsonFormat = jsonFormat;
    }

    internal async Task<IReadOnlyCollection<Id>> GetRowIdsAsync(CancellationToken cancellationToken)
    {
        if(!_isLoaded)
        {
            await LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        return _rowIds.Keys.ToArray();
    }

    internal async Task AddRowAsync(Id rowId, CancellationToken cancellationToken)
    {
        if(_rowIds.ContainsKey(rowId))
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if(_rowIds.ContainsKey(rowId))
            {
                return;
            }

            var record = new ReportIndexRecord { ReportId = rowId.ToString() };
            var data = JsonSerializer.SerializeToUtf8Bytes(record, _jsonFormat.Options);
            var metadata = JsonSerializer.SerializeToUtf8Bytes(new ExternalReportIndexMetadata(), _jsonFormat.Options);
            var expected = _expectedRevision ?? StreamRevision.None;
            var result = await _eventStoreService.AppendToStreamAsync(GetStreamName(), expected, new EventData(Uuid.NewUuid(), EventTypeName, data, metadata), cancellationToken).ConfigureAwait(false);

            _expectedRevision = result.NextExpectedStreamVersion;
            _rowIds.TryAdd(rowId, 0);
            _isLoaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if(_isLoaded)
            {
                return;
            }

            var streamName = GetStreamName();
            var start = StreamPosition.Start;

            while(true)
            {
                var batch = await _eventStoreService.ReadStreamEventsAsync(streamName, start, ReadBatchSize, cancellationToken).ConfigureAwait(false);

                if(batch.Events.Count == 0)
                {
                    break;
                }

                foreach(var resolved in batch.Events)
                {
                    var record = DeserializeRecord(resolved.Event.Data.Span);

                    if(record is null)
                    {
                        continue;
                    }

                    if(Id.TryFrom(record.ReportId, out var id))
                    {
                        _rowIds.TryAdd(id, 0);
                    }

                    _expectedRevision = new StreamRevision(resolved.Event.EventNumber);
                }

                if(batch.Events.Count < ReadBatchSize)
                {
                    break;
                }

                var last = batch.Events[^1].Event.EventNumber;
                start = new StreamPosition(last + 1);
            }

            _isLoaded = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    string GetStreamName() => $"{_reportType.DeclaredType.FullName}-index";

    ReportIndexRecord? DeserializeRecord(ReadOnlySpan<byte> data)
    {
        if(data.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReportIndexRecord>(data, _jsonFormat.Options);
        }
        catch(JsonException)
        {
            return null;
        }
    }

    sealed class ReportIndexRecord
    {
        public string ReportId { get; set; } = string.Empty;
    }

    sealed class ExternalReportIndexMetadata
    {
    }
}
