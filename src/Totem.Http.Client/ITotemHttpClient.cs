namespace Totem;

public interface ITotemHttpClient
{
    Task<IHttpCommandContext<IHttpCommand>> SendAsync(HttpCommandEnvelope command, CancellationToken cancellationToken);
    Task<IHttpReportQueryContext<IHttpReportQuery>> SendAsync(HttpReportQueryEnvelope query, CancellationToken cancellationToken);
    Task<IHttpReportListQueryContext<IHttpReportListQuery>> SendAsync(HttpReportListQueryEnvelope query, CancellationToken cancellationToken);
}
