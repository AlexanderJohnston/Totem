namespace Totem.Http;

public sealed class HttpClientAdapter : IHttpClientAdapter
{
    readonly HttpClient _httpClient;

    public HttpClientAdapter(HttpClient httpClient) =>
        _httpClient = httpClient;

    public Task SendAsync(IHttpClientAdapterRequest message, CancellationToken cancellationToken) =>
        message.SendAsync(_httpClient, cancellationToken);
}
