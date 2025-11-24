using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Totem;
using Totem.Core;
using Totem.InExternal.Services;
using Totem.InMemory.Reports;
using Totem.Reports;

namespace Totem.InExternal.Reports;

public sealed class ExternalReportStore : IReportStore, IReportReader
{
    readonly ConcurrentDictionary<IReportTransaction, ExternalReportStream> _streamsByTransaction = new();
    readonly ConcurrentDictionary<ReportType, ExternalReportIndex> _indexesByReport = new();
    readonly ConcurrentDictionary<ReportType, TimelineVersion> _latestVersions = new();
    readonly IAbstractEventStoreService _eventStoreService;
    readonly IServiceProvider _services;
    readonly IInMemoryReportBroker _broker;
    readonly TotemJsonFormat _jsonFormat;
    readonly RuntimeMap _map;

    public ExternalReportStore(
        IAbstractEventStoreService eventStoreService,
        IServiceProvider services,
        IInMemoryReportBroker broker,
        TotemJsonFormat jsonFormat,
        RuntimeMap map)
    {
        _eventStoreService = eventStoreService;
        _services = services;
        _broker = broker;
        _jsonFormat = jsonFormat;
        _map = map;
    }

    public async Task<IReportTransaction> StartTransactionAsync(IReportContext<IEvent> context, CancellationToken cancellationToken)
    {
        var report = (IReport) _services.GetRequiredService(context.ReportType.DeclaredType);

        if(report is ITimelineInit init)
        {
            init.TimelineId = context.ReportId;

            if(report.Row is IReportRowInit rowInit)
            {
                rowInit.Id = context.ReportId;
            }
        }

        var stream = new ExternalReportStream(context.ReportType, context.ReportKey, _eventStoreService, _jsonFormat, _map);
        var version = await stream.LoadAsync(report, cancellationToken).ConfigureAwait(false);

        context.ReportType.CallGivenIfDefined(report, context);

        var transaction = new ReportTransaction(this, context, report, version, cancellationToken);

        if(!_streamsByTransaction.TryAdd(transaction, stream))
        {
            throw new InvalidOperationException("Failed to register external report stream for transaction.");
        }

        return transaction;
    }

    public async Task CommitAsync(IReportTransaction transaction, CancellationToken cancellationToken)
    {
        if(!_streamsByTransaction.TryRemove(transaction, out var stream))
        {
            throw new InvalidOperationException("Missing external report stream for transaction.");
        }

        var result = await stream.CommitAsync(transaction, cancellationToken).ConfigureAwait(false);
        var reportType = transaction.Context.ReportType;
        var reportId = transaction.Context.ReportId;
        var newVersion = new TimelineVersion(reportId, result.Position);

        _latestVersions[reportType] = newVersion;
        _broker.PublishChanged(reportType, newVersion);

        if(result.IsNewRow)
        {
            var index = _indexesByReport.GetOrAdd(reportType, type => new ExternalReportIndex(type, _eventStoreService, _jsonFormat));
            await index.AddRowAsync(reportId, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task RollbackAsync(IReportTransaction transaction, CancellationToken cancellationToken)
    {
        _streamsByTransaction.TryRemove(transaction, out _);
        return Task.CompletedTask;
    }

    public async Task<ReportReadResult> ReadAsync(ReportType report, Id id, TimelineVersion? checkpoint, CancellationToken cancellationToken)
    {
        var stream = new ExternalReportStream(report, new TimelineKey(report.DeclaredType, id), _eventStoreService, _jsonFormat, _map);
        var snapshot = await stream.ReadSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if(snapshot is null)
        {
            return new ReportReadResult(new TimelineVersion(id));
        }

        var version = new TimelineVersion(id, snapshot.Value.Position);

        if(checkpoint is not null && checkpoint == version)
        {
            return new ReportReadResult(version, null);
        }

        var row = stream.DeserializeRow(snapshot.Value.RowJson);

        return new ReportReadResult(version, row);
    }

    public async Task<ReportListReadResult> ReadListAsync(ReportType report, TimelineVersion? checkpoint, CancellationToken cancellationToken)
    {
        var index = _indexesByReport.GetOrAdd(report, type => new ExternalReportIndex(type, _eventStoreService, _jsonFormat));
        var rowIds = await index.GetRowIdsAsync(cancellationToken).ConfigureAwait(false);

        var rows = new List<IReportRow>();
        TimelineVersion latestVersion = _latestVersions.TryGetValue(report, out var storedVersion) ? storedVersion : TimelineVersion.EmptyList;
        DateTimeOffset latestUpdated = DateTimeOffset.MinValue;

        foreach(var rowId in rowIds)
        {
            var stream = new ExternalReportStream(report, new TimelineKey(report.DeclaredType, rowId), _eventStoreService, _jsonFormat, _map);
            var snapshot = await stream.ReadSnapshotAsync(cancellationToken).ConfigureAwait(false);

            if(snapshot is null)
            {
                continue;
            }

            var version = new TimelineVersion(rowId, snapshot.Value.Position);

            if(checkpoint is null || checkpoint != version)
            {
                var row = stream.DeserializeRow(snapshot.Value.RowJson);

                if(row is not null)
                {
                    rows.Add(row);
                }
            }

            if(snapshot.Value.WhenUpdated > latestUpdated)
            {
                latestUpdated = snapshot.Value.WhenUpdated;
                latestVersion = version;
            }
        }

        _latestVersions[report] = latestVersion;

        var hasChanges = rows.Count > 0;

        if(!hasChanges && checkpoint is not null && checkpoint == latestVersion)
        {
            return new ReportListReadResult(latestVersion, null);
        }

        var rowArray = Array.CreateInstance(report.Row.DeclaredType, rows.Count);

        for(var i = 0; i < rows.Count; i++)
        {
            rowArray.SetValue(rows[i], i);
        }

        var typedRows = (IReadOnlyList<IReportRow>) rowArray;

        return new ReportListReadResult(latestVersion, typedRows);
    }
}
