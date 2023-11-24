namespace Totem.Tsp;

public sealed class TspHubConnectionService : IHostedService
{
    readonly ITspHubConnection _connection;

    public TspHubConnectionService(ITspHubConnection connection) =>
        _connection = connection;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _connection.ConnectAsync(cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken) =>
        await _connection.DisposeAsync();
}
