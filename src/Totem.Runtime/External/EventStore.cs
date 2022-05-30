using System.Text.Json;
using EventStore.Client;
using Totem.Core;
using Totem.Map;
using EventData = EventStore.Client.EventData;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.Toolkit.HighPerformance;
using Totem.Events;
using Totem.InMemory;
using System.Text.Json.Serialization;
using System.Text;

namespace Totem.External;
public sealed class EventStore : IExternalEventSubscription, IInMemoryEventSubscription
{
    readonly EventStoreClient _client;
    readonly ILogger _logger;
    readonly JsonSerializerOptions _options;

    public EventStore(ILogger<EventStore> logger, EventStoreClientSettings settings, JsonSerializerOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = new EventStoreClient(settings);
        _options = options;
    }

    public void Publish(IEventEnvelope envelope)
    {
        if(envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        _logger.LogTrace("[eventstore] Publish {@EventType}.{@EventId}", envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);

        Write(envelope);
    }

    public void Write([NotNull] IEventEnvelope envelope)
    {
        var data = new EventData(
            Uuid.NewUuid(),
            envelope.MessageKey.DeclaredType.FullName.ToString(),
            JsonSerializer.SerializeToUtf8Bytes((object)envelope.Message, _options),
            JsonSerializer.SerializeToUtf8Bytes((object)new EnvelopeMetaData(envelope.MessageKey, envelope.CorrelationId, envelope.Info.DeclaredType, envelope.Principal, envelope.WhenOccurred), _options)
        );
        _logger.LogTrace("[evenstore] Broadcast {@EventType}.{@EventId}", envelope.MessageKey.DeclaredType  , envelope.MessageKey.Id);
        var stream = "totem:" + envelope.MessageKey.DeclaredType.FullName;
        _client.AppendToStreamAsync(stream, StreamState.Any, new[] { data });
    }
}
