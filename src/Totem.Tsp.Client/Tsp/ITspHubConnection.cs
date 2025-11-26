namespace Totem.Tsp;

public interface ITspHubConnection : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task SubscribeAsync(TspSubscriptionEnvelope subscription, CancellationToken cancellationToken);
}
