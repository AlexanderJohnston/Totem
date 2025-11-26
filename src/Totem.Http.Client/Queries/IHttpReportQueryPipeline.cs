namespace Totem.Queries;

public interface IHttpReportQueryPipeline
{
    Task<IHttpReportQueryContext<IHttpReportQuery>> RunAsync(HttpReportQueryEnvelope envelope, CancellationToken cancellationToken);
}
