namespace Totem.Queries;

public sealed class HttpReportQueryRequest : IHttpClientAdapterRequest
{
    readonly IHttpReportQueryContext<IHttpReportQuery> _context;
    readonly IHttpReportQueryNegotiator _negotiator;

    public HttpReportQueryRequest(IHttpReportQueryContext<IHttpReportQuery> context, IHttpReportQueryNegotiator negotiator)
    {
        _context = context;
        _negotiator = negotiator;
    }

    public async Task SendAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var request = _negotiator.Negotiate(_context);

        var response = await client.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        _context.Response = await HttpClientAdapterResponse.CreateAsync(response, cancellationToken);

        _negotiator.NegotiateResult(_context);
    }
}
