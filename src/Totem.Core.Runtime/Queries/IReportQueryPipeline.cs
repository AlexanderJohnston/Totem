namespace Totem.Queries;

public interface IReportQueryPipeline
{
    Task<IReportQueryContext<IReportQuery>> RunAsync(ReportQueryEnvelope envelope, CancellationToken cancellationToken);
}
