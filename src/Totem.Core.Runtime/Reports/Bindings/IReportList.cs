namespace Totem.Reports.Bindings;

public interface IReportList<TRow> : IEnumerable<TRow>
{
    int Count { get; }
    TRow this[Id reportId] { get; }
    IEnumerable<Id> ReportIds { get; }

    bool ContainsReportId(Id reportId);
    bool TryGetRow(Id reportId, [NotNullWhen(true)] out TRow? row);
}
