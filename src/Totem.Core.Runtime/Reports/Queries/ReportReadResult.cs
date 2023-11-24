namespace Totem.Reports.Queries;

public sealed class ReportReadResult
{
    public ReportReadResult(TimelineVersion version, IReportRow? row = null)
    {
        Version = version;
        Row = row;
    }

    public TimelineVersion Version { get; }
    public IReportRow? Row { get; }
}
