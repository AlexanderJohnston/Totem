namespace Totem.InMemory;

internal sealed class InMemoryWorkflowCommandQueue : IDisposable
{
    readonly Channel<WorkflowCommandEnvelope> _channel = Channel.CreateUnbounded<WorkflowCommandEnvelope>(new UnboundedChannelOptions
    {
        SingleReader = true
    });
    readonly CancellationTokenSource _cancellation = new();
    readonly ILogger _logger;
    readonly string _name;
    readonly ICommandPipeline _pipeline;

    internal InMemoryWorkflowCommandQueue(ILogger<InMemoryWorkflowCommandQueue> logger, string name, ICommandPipeline pipeline)
    {
        _logger = logger;
        _name = name;
        _pipeline = pipeline;

        Task.Run(ObserveAsync);
    }

    internal void Enqueue(IReadOnlyList<WorkflowCommandEnvelope> newCommands)
    {
        lock(_channel)
        {
            foreach(var newCommand in newCommands)
            {
                _channel.Writer.TryWrite(newCommand);
            }
        }
    }

    public void Dispose()
    {
        _logger.LogTrace("Close command queue {QueueName:l}", _name);

        _channel.Writer.Complete();
        _cancellation.Cancel();
    }

    async Task ObserveAsync()
    {
        try
        {
            while(await _channel.Reader.WaitToReadAsync(_cancellation.Token))
            {
                if(_channel.Reader.TryRead(out var command))
                {
                    await RunPipelineAsync(command);
                }

                _logger.LogTrace("Waiting for next command from queue {QueueName:l}...", _name);
            }
        }
        catch(OperationCanceledException)
        { }
        catch(ChannelClosedException)
        { }
        catch(Exception exception)
        {
            _logger.LogError(exception, "Queue {QueueName:l} failed and has closed", _name);
        }
    }

    async Task RunPipelineAsync(WorkflowCommandEnvelope command)
    {
        var context = await _pipeline.RunAsync(command.ToCommandEnvelope(), _cancellation.Token);

        context.ExpectNoErrors();
    }
}
