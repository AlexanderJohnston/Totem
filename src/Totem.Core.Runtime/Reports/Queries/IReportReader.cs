namespace Totem.Reports.Queries;

public interface IReportReader
{
    Task<ReportReadResult> ReadAsync(ReportType report, Id id, TimelineVersion? checkpoint, CancellationToken cancellationToken);
    Task<ReportListReadResult> ReadListAsync(ReportType report, TimelineVersion? checkpoint, CancellationToken cancellationToken);
}
