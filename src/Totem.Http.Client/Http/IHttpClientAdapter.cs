namespace Totem.Http;

public interface IHttpClientAdapter
{
    Task SendAsync(IHttpClientAdapterRequest request, CancellationToken cancellationToken);
}
