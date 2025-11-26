namespace Totem.InMemory.Reports;

internal sealed class InMemoryReportStreamList
{
    readonly Dictionary<Id, InMemoryReportStream> _streamsById = new();
    readonly InMemoryReportStore _store;
    readonly ReportType _report;
    readonly IServiceProvider _services;
    readonly TotemJsonFormat _jsonFormat;
    readonly RuntimeMap _map;
    TimelineVersion _version;

    internal InMemoryReportStreamList(InMemoryReportStore store, ReportType report, IServiceProvider services, TotemJsonFormat jsonFormat, RuntimeMap map)
    {
        _store = store;
        _report = report;
        _services = services;
        _jsonFormat = jsonFormat;
        _map = map;
        _version = TimelineVersion.EmptyList;
    }

    internal IReportTransaction StartTransaction(IReportContext<IEvent> context, CancellationToken cancellationToken)
    {
        var report = (IReport) _services.GetRequiredService(_report.DeclaredType);

        if(report is ITimelineInit init)
        {
            init.TimelineId = context.ReportId;

            if(report.Row is IReportRowInit rowInit)
            {
                rowInit.Id = context.ReportId;
            }
        }

        var version = _streamsById.TryGetValue(context.ReportId, out var stream)
            ? stream.Load(report)
            : TimelinePosition.Start;

        _report.CallGivenIfDefined(report, context);

        return new ReportTransaction(_store, context, report, version, cancellationToken);
    }

    internal TimelineVersion Commit(IReportTransaction transaction)
    {
        var reportType = transaction.Context.ReportType;
        var reportId = transaction.Context.ReportId;

        if(!_streamsById.TryGetValue(reportId, out var stream))
        {
            stream = new InMemoryReportStream(reportType, reportId, _jsonFormat, _map);

            _streamsById[reportId] = stream;
        }

        var position = stream.Commit(transaction);

        _version = new TimelineVersion(reportId, position);

        return _version;
    }

    internal ReportReadResult Read(Id reportId, TimelineVersion? checkpoint) =>
        _streamsById.TryGetValue(reportId, out var stream)
            ? stream.Read(checkpoint?.Position)
            : new ReportReadResult(new TimelineVersion(reportId));
        
    internal ReportListReadResult ReadList(TimelineVersion? checkpoint)
    {
        var rows = null as IReadOnlyList<IReportRow>;

        if(checkpoint != _version)
        {
            var rowItems = _streamsById.Values.Select(stream => stream.ReadRow()).ToArray();
            var rowArray = Array.CreateInstance(_report.Row.DeclaredType, rowItems.Length);

            rowItems.CopyTo(rowArray, 0);

            rows = (IReadOnlyList<IReportRow>) rowArray;
        }

        return new ReportListReadResult(_version, rows);
    }
}
