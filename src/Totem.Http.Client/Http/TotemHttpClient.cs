namespace Totem.Http;

public sealed class TotemHttpClient : ITotemHttpClient
{
    readonly IHttpCommandPipeline _commandPipeline;
    readonly IHttpReportQueryPipeline _reportQueryPipeline;
    readonly IHttpReportListQueryPipeline _reportListQueryPipeline;

    public TotemHttpClient(
        IHttpCommandPipeline commandPipeline,
        IHttpReportQueryPipeline reportQueryPipeline,
        IHttpReportListQueryPipeline reportListQueryPipeline)
    {
        _commandPipeline = commandPipeline;
        _reportQueryPipeline = reportQueryPipeline;
        _reportListQueryPipeline = reportListQueryPipeline;
    }

    public Task<IHttpCommandContext<IHttpCommand>> SendAsync(HttpCommandEnvelope command, CancellationToken cancellationToken) =>
        _commandPipeline.RunAsync(command, cancellationToken);

    public Task<IHttpReportQueryContext<IHttpReportQuery>> SendAsync(HttpReportQueryEnvelope query, CancellationToken cancellationToken) =>
        _reportQueryPipeline.RunAsync(query, cancellationToken);

    public Task<IHttpReportListQueryContext<IHttpReportListQuery>> SendAsync(HttpReportListQueryEnvelope query, CancellationToken cancellationToken) =>
        _reportListQueryPipeline.RunAsync(query, cancellationToken);
}
