namespace Totem.Http;

public interface IHttpClientAdapterRequest
{
    Task SendAsync(HttpClient client, CancellationToken cancellationToken);
}
