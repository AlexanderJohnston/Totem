using Totem.Queues;

namespace Totem.External;
public class ExternalQueueClient : IQueueClient, IDisposable
{
    public void Dispose() => throw new NotImplementedException();
    public Task EnqueueAsync(IQueueCommandEnvelope envelope, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task EnqueueAsync(IEnumerable<IQueueCommandEnvelope> envelopes, CancellationToken cancellationToken) => throw new NotImplementedException();
}
