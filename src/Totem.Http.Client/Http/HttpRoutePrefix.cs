namespace Totem.Http;

public sealed class HttpRoutePrefix : IHttpRoutePrefix
{
    readonly string _prefix;

    public HttpRoutePrefix(string prefix) =>
        _prefix = !string.IsNullOrWhiteSpace(prefix) ? prefix : throw new ArgumentOutOfRangeException(nameof(prefix));

    public string Apply(string route) =>
        $"{_prefix}/{route.ToString().TrimStart('/')}";
}
