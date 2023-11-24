namespace Totem.Commands;

public sealed class HttpCommandNegotiator : IHttpCommandNegotiator
{
    readonly IHttpRoutePrefix _routePrefix;
    readonly TotemJsonFormat _jsonFormat;

    public HttpCommandNegotiator(IHttpRoutePrefix routePrefix, TotemJsonFormat jsonFormat)
    {
        _routePrefix = routePrefix;
        _jsonFormat = jsonFormat;
    }

    public HttpRequestMessage Negotiate(IHttpCommandContext<IHttpCommand> context)
    {
        var route = HttpMessageRoutes.Command(context.CommandInfo.ExternalType);
        var json = JsonSerializer.Serialize(context.Command, _jsonFormat.Options);

        return new HttpRequestMessage(HttpMethod.Post, _routePrefix.Apply(route))
        {
            Content = new StringContent(json, null, ContentTypes.Json)
        };
    }
}
