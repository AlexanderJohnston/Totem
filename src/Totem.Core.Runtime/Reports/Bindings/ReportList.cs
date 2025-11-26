namespace Totem.Reports.Bindings;

public sealed class ReportList<TRow> : IReportList<TRow>
    where TRow : IReportRow
{
    readonly IReadOnlyDictionary<Id, TRow> _rowsByReportId;

    public ReportList(IEnumerable<TRow> queryResult) =>
        _rowsByReportId = queryResult.ToDictionary(row => row.Id);

    public TRow this[Id reportId] => _rowsByReportId[reportId];
    public int Count => _rowsByReportId.Count;
    public IEnumerable<Id> ReportIds => _rowsByReportId.Keys;

    public IEnumerator<TRow> GetEnumerator() => _rowsByReportId.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsReportId(Id reportId) =>
        _rowsByReportId.ContainsKey(reportId);

    public bool TryGetRow(Id reportId, [NotNullWhen(true)] out TRow? row) =>
        _rowsByReportId.TryGetValue(reportId, out row);
}
