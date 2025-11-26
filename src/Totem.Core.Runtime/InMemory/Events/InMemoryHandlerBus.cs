namespace Totem.InMemory.Events;

public sealed class InMemoryHandlerBus : IHandlerBus
{
    readonly Channel<EventEnvelope> _channel = Channel.CreateUnbounded<EventEnvelope>(new UnboundedChannelOptions
    {
        SingleReader = true
    });
    readonly CancellationTokenSource _cancellation = new();
    readonly ILogger _logger;
    readonly IEventHandlerPipeline _pipeline;

    public InMemoryHandlerBus(ILogger<InMemoryHandlerBus> logger, IEventHandlerPipeline pipeline)
    {
        _logger = logger;
        _pipeline = pipeline;

        Task.Run(ObserveAsync);
    }

    public Task PublishAsync(EventEnvelope newEvent, CancellationToken cancellationToken)
    {
        lock(_channel)
        {
            _channel.Writer.TryWrite(newEvent);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _logger.LogTrace("Close event handler bus");

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
            }
        }
        catch(OperationCanceledException)
        { }
        catch(ChannelClosedException)
        { }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Event handler bus failed and has closed");
        }
    }

    async Task RunPipelineAsync(EventEnvelope e)
    {
        var context = await _pipeline.RunAsync(e, _cancellation.Token);

        context.ExpectNoErrors();
    }
}
