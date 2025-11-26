namespace Totem.InMemory.Events;

public sealed class InMemoryEventBus : IInMemoryEventBus, IDisposable
{
    readonly Channel<EventEnvelope> _channel = Channel.CreateUnbounded<EventEnvelope>(new UnboundedChannelOptions
    {
        SingleReader = true
    });
    readonly CancellationTokenSource _cancellation = new();
    readonly ILogger _logger;
    readonly IEventPipeline _pipeline;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger, IEventPipeline pipeline)
    {
        _logger = logger;
        _pipeline = pipeline;

        Task.Run(ObserveAsync);
    }

    public void Publish(IReadOnlyList<EventEnvelope> newEvents)
    {
        lock(_channel)
        {
            foreach(var newEvent in newEvents)
            {
                _channel.Writer.TryWrite(newEvent);
            }
        }
    }

    public void Dispose()
    {
        _logger.LogTrace("Close event bus");

        _channel.Writer.Complete();
        _cancellation.Cancel();
    }

    async Task ObserveAsync()
    {
        try
        {
            while(await _channel.Reader.WaitToReadAsync(_cancellation.Token))
            {
                if(_channel.Reader.TryRead(out var e))
                {
                    await RunPipelineAsync(e);
                }

                _logger.LogTrace("Waiting for next event...");
            }
        }
        catch(OperationCanceledException)
        { }
        catch(ChannelClosedException)
        { }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Event bus failed and has closed");
        }
    }

    async Task RunPipelineAsync(EventEnvelope e)
    {
        var context = await _pipeline.RunAsync(e, _cancellation.Token);

        context.ExpectNoErrors();
    }
}
