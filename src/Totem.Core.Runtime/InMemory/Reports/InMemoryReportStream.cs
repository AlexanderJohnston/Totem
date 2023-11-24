using System.Text.Json;

namespace Totem.InMemory.Reports;

internal sealed class InMemoryReportStream
{
    readonly List<IEventContext<IEvent>> _events = new();
    readonly ReportType _reportType;
    readonly Id _reportId;
    readonly TotemJsonFormat _jsonFormat;
    readonly RuntimeMap _map;
    TimelinePosition _position;
    string _json = null!;

    internal InMemoryReportStream(ReportType reportType, Id reportId, TotemJsonFormat jsonFormat, RuntimeMap map)
    {
        _reportType = reportType;
        _reportId = reportId;
        _jsonFormat = jsonFormat;
        _map = map;
    }

    internal TimelinePosition Load(IReport report)
    {
        foreach(var e in _events)
        {
            _reportType.CallGivenIfDefined(report, e);

            _position = _position.Next();
        }

        return _position;
    }

    internal TimelinePosition Commit(IReportTransaction transaction)
    {
        transaction.Context.ReportKey.CheckConcurrency(_position, transaction.Position);

        var persistedEvent = _map.CreateContext(transaction.Context.Envelope);

        _events.Add(persistedEvent);

        _json = JsonSerializer.Serialize(transaction.Report.Row, _reportType.Row.DeclaredType, _jsonFormat.Options);

        _position = _position.Next();

        return _position;
    }

    internal ReportReadResult Read(TimelinePosition? checkpoint)
    {
        var row = checkpoint is null || checkpoint != _position ? ReadRow() : null;

        return new ReportReadResult(new TimelineVersion(_reportId, _position), row);
    }

    internal IReportRow ReadRow() =>
        (IReportRow) JsonSerializer.Deserialize(_json, _reportType.Row.DeclaredType, _jsonFormat.Options)!;
}
