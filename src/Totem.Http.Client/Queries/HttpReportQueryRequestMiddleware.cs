namespace Totem.Queries;

public sealed class HttpReportQueryRequestMiddleware : IHttpReportQueryMiddleware
{
    readonly IHttpClientAdapter _client;
    readonly IHttpReportQueryNegotiator _negotiator;

    public HttpReportQueryRequestMiddleware(IHttpClientAdapter client, IHttpReportQueryNegotiator negotiator)
    {
        _client = client;
        _negotiator = negotiator;
    }

    public async Task InvokeAsync(IHttpReportQueryContext<IHttpReportQuery> context, Func<Task> next, CancellationToken cancellationToken)
    {
        await _client.SendAsync(new HttpReportQueryRequest(context, _negotiator), cancellationToken);

        await next();
    }
}
