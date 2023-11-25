//using EventStore.Client;
//using Grpc.Core;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Mime;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Totem.Map;

//namespace Totem.InExternal.Services
//{
//    public class AbstractEventStoreService : IAbstractEventStoreService
//    {
//        private readonly ILogger<EventStoreService> _logger;
//        private readonly EventStoreClient _client;
//        private readonly IEventSerializer _eventSerializer;
//        private readonly IMetadataSerializer _metadataSerializer;

//        public AbstractEventStoreService(
//            EventStoreClient client,
//            IEventSerializer eventSerializer,
//            IMetadataSerializer metadataSerializer,
//            ILogger<EventStoreService> logger
//        )
//        {
//            _logger = logger;
//            _client = client;
//            _eventSerializer = eventSerializer;
//            _metadataSerializer = metadataSerializer;
//        }

//        public async Task<bool> StreamExistsAsync(string streamName, CancellationToken cancellationToken)
//        {
//            var read = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, 1, cancellationToken: cancellationToken);
//            return (await read.ReadState).Equals(ReadState.Ok);
//        }

//        public async Task<AppendResult> AppendToStreamAsync(string streamName, StreamRevision expectedVersion, IEnumerable<EventData> events, CancellationToken cancellationToken)
//        {
//            var proposedEvents = events.Select(e =>
//            {
//                var (eventType, contentType, payload) = _eventSerializer.SerializeEvent(e);
//                var metadata = _metadataSerializer.Serialize(e.Metadata);
//                return new EventData(Uuid.NewUuid(), eventType, payload, metadata, contentType);
//            });

//            try
//            {
//                var writeResult = await _client.AppendToStreamAsync(streamName, expectedVersion, proposedEvents, cancellationToken: cancellationToken);
//                return new AppendResult
//                {
//                    NextExpectedStreamVersion = writeResult.NextExpectedStreamRevision,
//                    LogPosition = writeResult.LogPosition.CommitPosition
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Unable to append events to {streamName}");
//                throw;
//            }
//        }

//        public async Task<ReadResult> ReadStreamEventsAsync(string streamName, StreamPosition start, int count, CancellationToken cancellationToken)
//        {
//            try
//            {
//                var read = _client.ReadStreamAsync(Direction.Forwards, streamName, start, count, cancellationToken: cancellationToken);
//                var resolvedEvents = await read.ToArrayAsync(cancellationToken);
//                var streamEvents = resolvedEvents.Select(re => _eventSerializer.DeserializeEvent(re.Event.Data.ToArray(), re.Event.EventType, re.Event.ContentType));

//                return new ReadResult { Events = streamEvents.ToArray() };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Unable to read events from {streamName}");
//                throw;
//            }
//        }

//        public async Task<ReadResult> ReadStreamEventsBackwardsAsync(string streamName, int count, CancellationToken cancellationToken)
//        {
//            try
//            {
//                var read = _client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, count, cancellationToken: cancellationToken);
//                var resolvedEvents = await read.ToArrayAsync(cancellationToken);
//                var streamEvents = resolvedEvents.Select(re => _eventSerializer.DeserializeEvent(re.Event.Data.ToArray(), re.Event.EventType, re.Event.ContentType));

//                return new ReadResult { Events = streamEvents.ToArray() };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Unable to read events from {streamName}");
//                throw;
//            }
//        }

//        public async Task TruncateStreamAsync(string streamName, StreamPosition truncatePosition, StreamRevision expectedVersion, CancellationToken cancellationToken)
//        {
//            // Implement logic for truncating stream from the truncatePosition here
//            // The logic will vary based on how you want to handle the truncation process and what support the library provides for this process.
//            throw new NotImplementedException("TruncateStreamAsync is not implemented yet.");
//        }

//        public async Task DeleteStreamAsync(string streamName, StreamRevision expectedVersion, CancellationToken cancellationToken)
//        {
//            try
//            {
//                await _client.TombstoneAsync(streamName, expectedVersion, cancellationToken: cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Unable to delete {streamName}");
//                throw;
//            }
//        }

//        public async Task<AppendResult> AppendToStreamAsync(string streamName, StreamRevision expectedVersion, EventData newEvent, CancellationToken cancellationToken)
//        {
//            try
//            {
//                var eventList = new List<EventData> { newEvent };
//                var writeResult = await _client.AppendToStreamAsync(streamName, expectedVersion, eventList, cancellationToken: cancellationToken);
//                return new AppendResult
//                {
//                    NextExpectedStreamVersion = writeResult.NextExpectedStreamRevision,
//                    LogPosition = writeResult.LogPosition.CommitPosition
//                };
//            }
//            catch(Exception ex)
//            {
//                _logger.LogError(ex, $"Unable to append events to {streamName}");
//                throw;
//            }
//        }
//    }
//}
