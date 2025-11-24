using EventStore.Client;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Totem.InExternal.Services;

public class AbstractEventStoreService : IAbstractEventStoreService
{
    readonly ILogger<AbstractEventStoreService> _logger;
    readonly EventStoreClient _client;

    public AbstractEventStoreService(EventStoreClient client, ILogger<AbstractEventStoreService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> StreamExistsAsync(string streamName, CancellationToken cancellationToken)
    {
        try
        {
            var read = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start, 1, cancellationToken: cancellationToken);
            var state = await read.ReadState.ConfigureAwait(false);
            return state == ReadState.Ok;
        }
        catch(StreamNotFoundException)
        {
            return false;
        }
        catch(RpcException ex) when(ex.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<AppendResult> AppendToStreamAsync(string streamName, StreamRevision expectedVersion, IEnumerable<EventData> events, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.AppendToStreamAsync(streamName, expectedVersion, events, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new AppendResult(result.NextExpectedStreamRevision, result.LogPosition.CommitPosition);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unable to append events to {Stream}", streamName);
            throw;
        }
    }

    public Task<AppendResult> AppendToStreamAsync(string streamName, StreamRevision expectedVersion, EventData newEvent, CancellationToken cancellationToken) =>
        AppendToStreamAsync(streamName, expectedVersion, new[] { newEvent }, cancellationToken);

    public Task<ReadResult> ReadStreamEventsAsync(string streamName, StreamPosition start, int count, CancellationToken cancellationToken) =>
        ReadAsync(Direction.Forwards, streamName, start, count, cancellationToken);

    public Task<ReadResult> ReadStreamEventsBackwardsAsync(string streamName, int count, CancellationToken cancellationToken) =>
        ReadAsync(Direction.Backwards, streamName, StreamPosition.End, count, cancellationToken);

    public Task TruncateStreamAsync(string streamName, StreamPosition truncatePosition, StreamRevision expectedVersion, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Truncating streams is not supported by this EventStore service implementation.");

    public async Task DeleteStreamAsync(string streamName, StreamRevision expectedVersion, CancellationToken cancellationToken)
    {
        try
        {
            await _client.DeleteAsync(streamName, expectedVersion, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unable to delete {Stream}", streamName);
            throw;
        }
    }

    async Task<ReadResult> ReadAsync(Direction direction, string streamName, StreamPosition start, int count, CancellationToken cancellationToken)
    {
        try
        {
            var events = new List<ResolvedEvent>();
            var read = _client.ReadStreamAsync(direction, streamName, start, count, cancellationToken: cancellationToken);

            await foreach(var resolvedEvent in read.WithCancellation(cancellationToken))
            {
                events.Add(resolvedEvent);
            }

            return new ReadResult(events);
        }
        catch(StreamNotFoundException)
        {
            return new ReadResult(Array.Empty<ResolvedEvent>());
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unable to read events from {Stream}", streamName);
            throw;
        }
    }
}
