namespace Totem.Queries;

public sealed class HttpReportListQueryRequestMiddleware : IHttpReportListQueryMiddleware
{
    readonly IHttpClientAdapter _client;
    readonly IHttpReportListQueryNegotiator _negotiator;

    public HttpReportListQueryRequestMiddleware(IHttpClientAdapter client, IHttpReportListQueryNegotiator negotiator)
    {
        _client = client;
        _negotiator = negotiator;
    }

    public async Task InvokeAsync(IHttpReportListQueryContext<IHttpReportListQuery> context, Func<Task> next, CancellationToken cancellationToken)
    {
        await _client.SendAsync(new HttpReportListQueryRequest(context, _negotiator), cancellationToken);

        await next();
    }
}
