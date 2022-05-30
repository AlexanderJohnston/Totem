using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Toolkit.HighPerformance;
using Totem.Core;
using Totem.Events;

namespace Totem.External
{
    public class HostedEventListener : BackgroundService
    {
        readonly ILogger<HostedEventListener> _logger;
        readonly IEventPipeline _pipeline;
        readonly CancellationTokenSource _cancel;
        readonly EventStoreClient _client;
        readonly JsonSerializerOptions _options;
        
        public HostedEventListener(ILogger<HostedEventListener> logger, IEventPipeline pipeline, EventStoreClientSettings settings, JsonSerializerOptions options) 
        {
            _logger = logger;
            _pipeline = pipeline;
            _cancel = new CancellationTokenSource();
            _client = new EventStoreClient(settings);
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await StartAsync(stoppingToken);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("[listener service] Subscribing to the $all stream in eventstore.");
            Task.Run(() => SubscribeToAll(_cancel.Token), _cancel.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancel.Cancel();
            _logger.LogDebug("[listenr service] This service is stopping.");
            return Task.CompletedTask;
        }

        public async Task SubscribeToAll(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            await _client.SubscribeToAllAsync(
                FromAll.Start,
                async (subscription, evnt, cancellationToken) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    _logger.LogTrace($"Received event {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
                    await HandleEvent(evnt);
                },
                filterOptions: new SubscriptionFilterOptions(EventTypeFilter.Prefix("totem:")),
                subscriptionDropped: (sub, reason, ex) => _logger.LogError("subscription: {@sub} failed for {@reason} with {@ex}", sub, reason, ex),
                cancellationToken: token
            );
        }

        IEvent? UnpackData(EventRecord evt) 
        {
            if (evt.Data.Length == 0)
            {
                _logger.LogError($"Attempted to unpack {evt.EventType}@{evt.EventId} but found no data.");
                throw new ArgumentNullException(nameof(evt.Data));
            }
            var stream = ReadOnlyMemoryExtensions.AsStream(evt.Data);
            try
            {
                var unboxed = JsonSerializer.Deserialize<IEvent>(stream, _options);
                return unboxed;
            }
            catch (JsonException jex)
            {
                _logger.LogError($"Couldn't parse the JSON Data for {evt.EventType}@{evt.EventId}.");
                throw jex;
            }
            return null;
        }
        EnvelopeMetaData? UnpackMeta(EventRecord evt)
        {
            if (evt.Metadata.Length == 0)
            {
                _logger.LogError($"Attempted to unpack ${evt.EventType}@${evt.EventId} but found no data.");
                throw new ArgumentNullException(nameof(evt.Metadata));
            }
            var stream = ReadOnlyMemoryExtensions.AsStream(evt.Metadata);
            try
            {
                var metadata = JsonSerializer.Deserialize<EnvelopeMetaData>(evt.Metadata.AsStream(), _options);
                return metadata;
            }
            catch (JsonException jex)
            {
                _logger.LogError($"Couldn't parse the JSON Meta Data for {evt.EventType}@{evt.EventId}.");
                throw jex;
            }
            return null;
        }

        async Task HandleEvent(ResolvedEvent evnt)
        {
            //if (evnt.OriginalStreamId.Contains('$'))
            //    return;
            _logger.LogTrace($"Attempting to unpack {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
            try
            {
                var data = UnpackData(evnt.Event);
                var meta = UnpackMeta(evnt.Event);
                var envelope = new EventEnvelope(
                new ItemKey(meta.EventType, meta.MessageKey.Id ?? Id.NewId()),
                data,
                EventInfo.From(meta.EventType),
                meta.CorrelationId ?? Id.NewId(),
                meta.Principal ?? new ClaimsPrincipal(),
                meta.WhenOccurred);
                await RunPipelineAsync(envelope);
            }
            catch (JsonException jex)
            {
                _logger.LogDebug($"Failed to unpack {evnt.OriginalEventNumber}@{evnt.OriginalStreamId}");
            }
            // finally
            
        }

        async Task RunPipelineAsync(IEventEnvelope envelope)
        {
            var context = await _pipeline.RunAsync(envelope, _cancel.Token);

            if (context.HasErrors)
            {
                _logger.LogError("[eventstore] Pipeline {@PipelineId} failed for {@EventType}.{@EventId}", _pipeline.Id, envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);
            }
            else
            {
                _logger.LogTrace("[eventstore] Pipeline {@PipelineId} succeeded at observing {@EventType}.{@EventId}", _pipeline.Id, envelope.MessageKey.DeclaredType, envelope.MessageKey.Id);
            }
        }
    }
}
