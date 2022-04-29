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
using System.Security.Claims;
using System.Text;

namespace Totem.External;
public sealed class EventStore : IExternalEventSubscription, IInMemoryEventSubscription
{
    readonly CancellationToken _cancel;
    readonly EventStoreClient _client;
    readonly RuntimeMap _map;
    readonly ILogger _logger;
    readonly IEventPipeline _pipeline;
    readonly JsonSerializerOptions _options;

    public EventStore(ILogger<EventStore> logger, IEventPipeline pipeline, RuntimeMap map, EventStoreClientSettings settings, JsonSerializerOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _client = new EventStoreClient(settings);
        _cancel = new CancellationToken();
        _options = options;
        //Task.Run(SubscribeToAll);
    }

    public void Publish(IEventEnvelope envelope)
    {
        if(envelope is null)
            throw new ArgumentNullException(nameof(envelope));

        _logger.LogTrace("[eventstore] Publish {@EventType}.{@EventId}", envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);

        Write(envelope);
    }

    public async Task SubscribeToAll()
    {
        await _client.SubscribeToAllAsync(
            FromAll.Start,
            async (subscription, evnt, cancellationToken) =>
            {
                _logger.LogTrace($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
                await HandleEvent(evnt);
            },
            filterOptions: new SubscriptionFilterOptions(EventTypeFilter.Prefix("totem:")),
            subscriptionDropped: (sub, reason, ex) => _logger.LogError("subscription: {@sub} failed for {@reason} with {@ex}", sub, reason, ex)

        );
    }

    async Task HandleEvent(ResolvedEvent evnt)
    {
        //var stream = ReadOnlyMemoryExtensions.AsStream(evnt.Event.Data);
        //var meta = ReadOnlyMemoryExtensions.AsStream(evnt.Event.Metadata);
        //if (evnt.OriginalStreamId.Contains('$'))
        //    return;
        //var unboxed = JsonSerializer.Deserialize<IEvent>(evnt.Event.Data.AsStream(), _options);
        //var test = Encoding.UTF8.GetString(evnt.Event.Metadata.ToArray());
        //var metadata = JsonSerializer.Deserialize<EnvelopeMetaData>(evnt.Event.Metadata.AsStream(), _options);
        //var envelope = new EventEnvelope(
        //    new ItemKey(metadata.EventType, metadata.MessageKey.Id ?? Id.NewId()),
        //    unboxed,
        //    EventInfo.From(metadata.EventType),
        //    metadata.CorrelationId ?? Id.NewId(),
        //    metadata.Principal ?? new ClaimsPrincipal(),
        //    metadata.WhenOccurred);
        return;
        
    }
    async Task RunPipelineAsync(IEventEnvelope envelope)
    {
        var context = await _pipeline.RunAsync(envelope, _cancel);

        if(context.HasErrors)
        {
            _logger.LogError("[eventstore] Pipeline {@PipelineId} failed for {@EventType}.{@EventId}", _pipeline.Id, envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);
        }
        else
        {
            _logger.LogTrace("[eventstore] Pipeline {@PipelineId} succeeded at observing {@EventType}.{@EventId}", _pipeline.Id, envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);
        }
    }

    public void Write([NotNull] IEventEnvelope envelope)
    {
        var data = new EventData(
            Uuid.NewUuid(),
            envelope.MessageKey.DeclaredType.Name.ToString(),
            JsonSerializer.SerializeToUtf8Bytes((object)envelope.Message, _options),
            JsonSerializer.SerializeToUtf8Bytes((object)new EnvelopeMetaData(envelope.MessageKey, envelope.CorrelationId, envelope.Info.DeclaredType, envelope.Principal, envelope.WhenOccurred), _options)
        );
        _logger.LogTrace("[evenstore] Broadcast {@EventType}.{@EventId}", envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);
        var stream = "totem:" + envelope.MessageKey.DeclaredType.Name;
        _client.AppendToStreamAsync(stream, StreamState.Any, new[] { data }, cancellationToken: _cancel);
    }

    public void Complete() => throw new NotImplementedException();
    public async Task ReadAsync() => throw new NotImplementedException();

}

public class EnvelopeMetaData
{
    //internal EnvelopeMetaData(IEventEnvelope envelope)
    //{
    //    MessageKey = envelope.MessageKey;
    //    CorrelationId = envelope.CorrelationId;
    //    Principal = envelope.Principal;
    //    EventType = envelope.Info.DeclaredType;
    //    WhenOccurred = envelope.WhenOccurred;
    //}

    public EnvelopeMetaData(ItemKey messageKey, Id correlationId, Type declaredType, ClaimsPrincipal principal, DateTimeOffset whenOccurred)
    {
        MessageKey = messageKey;
        CorrelationId = correlationId;
        Principal = principal;
        EventType = declaredType;
        WhenOccurred = whenOccurred;
    }

    public Type EventType { get; }

    public ItemKey MessageKey { get; }
    public Id CorrelationId { get; }
    public ClaimsPrincipal Principal { get; }
    public DateTimeOffset WhenOccurred { get; }
}
