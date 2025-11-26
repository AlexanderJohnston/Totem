namespace Totem.Queries;

public sealed class HttpReportListQueryRequest : IHttpClientAdapterRequest
{
    readonly IHttpReportListQueryContext<IHttpReportListQuery> _context;
    readonly IHttpReportListQueryNegotiator _negotiator;

    public HttpReportListQueryRequest(IHttpReportListQueryContext<IHttpReportListQuery> context, IHttpReportListQueryNegotiator negotiator)
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
