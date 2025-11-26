namespace Totem.InMemory.Events;

public sealed class InMemoryReportBus : IReportBus, IDisposable
{
    record Observation(EventEnvelope Event, ObserverRoute Route);

    readonly Channel<Observation> _channel = Channel.CreateUnbounded<Observation>(new UnboundedChannelOptions
    {
        SingleReader = true
    });
    readonly CancellationTokenSource _cancellation = new();
    readonly ILogger _logger;
    readonly IReportPipeline _pipeline;

    public InMemoryReportBus(ILogger<InMemoryReportBus> logger, IReportPipeline pipeline)
    {
        _logger = logger;
        _pipeline = pipeline;

        Task.Run(ObserveAsync);
    }

    public Task PublishAsync(EventEnvelope newEvent, IEnumerable<ObserverRoute> routes, CancellationToken cancellationToken)
    {
        lock(_channel)
        {
            foreach(var route in routes)
            {
                _channel.Writer.TryWrite(new(newEvent, route));
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _logger.LogTrace("Close report bus");

        _channel.Writer.Complete();
        _cancellation.Cancel();
    }

    async Task ObserveAsync()
    {
        try
        {
            while(await _channel.Reader.WaitToReadAsync(_cancellation.Token))
            {
                if(_channel.Reader.TryRead(out var observation))
                {
                    await RunPipelineAsync(observation);
                }

                _logger.LogTrace("Waiting for next report route...");
            }
        }
        catch(OperationCanceledException)
        { }
        catch(ChannelClosedException)
        { }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Report bus failed and has closed");
        }
    }

    async Task RunPipelineAsync(Observation observation)
    {
        var context = await _pipeline.RunAsync(observation.Event, observation.Route, _cancellation.Token);

        context.ExpectNoErrors();
    }
}
