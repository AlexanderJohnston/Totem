namespace Totem.Hosting;

public sealed class SameOriginCredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.SameOrigin);

        return base.SendAsync(request, cancellationToken);
    }
}
