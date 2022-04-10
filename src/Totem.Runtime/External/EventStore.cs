using System.Text.Json;
using EventStore.Client;
using Totem.Core;
using Totem.Map;
using EventData = EventStore.Client.EventData;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Microsoft.Toolkit.HighPerformance;
using Totem.Events;

namespace Totem.External;
public sealed class EventStore
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
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Task.Run(SubscribeToAll);
    }
    public async Task ReadAsync() => throw new NotImplementedException();

    public async Task SubscribeToAll()
    {
        await _client.SubscribeToAllAsync(
            FromAll.Start,
            async (subscription, evnt, cancellationToken) => {
                _logger.LogTrace($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
                await HandleEvent(evnt);
            }
        );
    }

    async Task HandleEvent(ResolvedEvent evnt)
    {
        var stream = ReadOnlyMemoryExtensions.AsStream(evnt.Event.Data);
        var unboxed = JsonSerializer.Deserialize<IEventEnvelope>(stream, _options);
        await RunPipelineAsync(unboxed);
    }
    async Task RunPipelineAsync(IEventEnvelope envelope)
    {
        var context = await _pipeline.RunAsync(envelope, _cancel);

        if(context.HasErrors)
        {
            _logger.LogError("[event] Pipeline {@PipelineId} failed for {@EventType}.{@EventId}", _pipeline.Id, envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);
        }
    }

    public async Task WriteAsync([NotNull] IEventEnvelope envelope)
    {
        var data = new EventData(
            Uuid.NewUuid(),
            envelope.MessageKey.DeclaredType.Name.ToString(),
            JsonSerializer.SerializeToUtf8Bytes(envelope)
        );
        _logger.LogTrace("[evenstore] Broadcast {@EventType}.{@EventId}", envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);
        var stream = envelope.MessageKey.DeclaredType.Name;
        await _client.AppendToStreamAsync(stream, StreamState.Any, new[] { data }, cancellationToken: _cancel);
    }

    public void Complete()
    {
        throw new NotImplementedException();
    }
}
