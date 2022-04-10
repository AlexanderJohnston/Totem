using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Totem.Core;
using Totem.Events;

namespace Totem.External;

public sealed class ExternalEventSubscription : IExternalEventSubscription, IDisposable
{
    readonly CancellationTokenSource _cancellation = new();
    readonly ILogger _logger;
    readonly EventStore _store;


    public ExternalEventSubscription(ILogger<ExternalEventSubscription> logger, EventStore store)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task Publish(IEventEnvelope envelope)
    {
        if(envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        _logger.LogTrace("[event] Publish {@EventType}.{@EventId}", envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);

        await _store.WriteAsync(envelope);
    }

    public void Dispose()
    {
        _logger.LogTrace("[events] Close EventStore subscription");

        _store.Complete();
        _cancellation.Cancel();
    }
}
