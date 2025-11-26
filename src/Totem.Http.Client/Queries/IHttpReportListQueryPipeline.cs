namespace Totem.Queries;

public interface IHttpReportListQueryPipeline
{
    Task<IHttpReportListQueryContext<IHttpReportListQuery>> RunAsync(HttpReportListQueryEnvelope envelope, CancellationToken cancellationToken);
}
