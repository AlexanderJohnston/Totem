namespace Totem.Reports.Queries;

public sealed class ReportListReadResult
{
    public ReportListReadResult(TimelineVersion version, IReadOnlyList<IReportRow>? rows)
    {
        Version = version;
        Rows = rows;
    }

    public TimelineVersion Version { get; }
    public IReadOnlyList<IReportRow>? Rows { get; }
}
