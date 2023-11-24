namespace Totem.Queries;

public interface IReportListQueryPipeline
{
    Task<IReportListQueryContext<IReportListQuery>> RunAsync(ReportListQueryEnvelope envelope, CancellationToken cancellationToken);
}
