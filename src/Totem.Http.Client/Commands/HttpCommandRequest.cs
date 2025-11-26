namespace Totem.Commands;

public sealed class HttpCommandRequest : IHttpClientAdapterRequest
{
    readonly IHttpCommandContext<IHttpCommand> _context;
    readonly IHttpCommandNegotiator _negotiator;

    public HttpCommandRequest(IHttpCommandContext<IHttpCommand> context, IHttpCommandNegotiator negotiator)
    {
        _context = context;
        _negotiator = negotiator;
    }

    public async Task SendAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var request = _negotiator.Negotiate(_context);
        var response = await client.SendAsync(request, cancellationToken);

        _context.Response = await HttpClientAdapterResponse.CreateAsync(response, cancellationToken);
    }
}
