namespace Totem.Commands;

public sealed class HttpCommandRequestMiddleware : IHttpCommandMiddleware
{
    readonly IHttpClientAdapter _client;
    readonly IHttpCommandNegotiator _negotiator;

    public HttpCommandRequestMiddleware(IHttpClientAdapter client, IHttpCommandNegotiator negotiator)
    {
        _client = client;
        _negotiator = negotiator;
    }

    public async Task InvokeAsync(IHttpCommandContext<IHttpCommand> context, Func<Task> next, CancellationToken cancellationToken)
    {
        await _client.SendAsync(new HttpCommandRequest(context, _negotiator), cancellationToken);

        context.ExpectNoErrors();

        await next();
    }
}
