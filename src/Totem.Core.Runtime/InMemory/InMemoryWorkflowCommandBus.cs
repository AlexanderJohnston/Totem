namespace Totem.InMemory;

public sealed class InMemoryWorkflowCommandBus : IInMemoryWorkflowCommandBus, IDisposable
{
    readonly ConcurrentDictionary<string, InMemoryWorkflowCommandQueue> _queuesByName = new();
    readonly ILoggerFactory _loggerFactory;
    readonly ICommandPipeline _pipeline;

    public InMemoryWorkflowCommandBus(ILoggerFactory loggerFactory, ICommandPipeline pipeline)
    {
        _loggerFactory = loggerFactory;
        _pipeline = pipeline;
    }

    public void Publish(IReadOnlyList<WorkflowCommandEnvelope> newCommands)
    {
        foreach(var newCommandsByQueue in newCommands.GroupBy(x => x.Queue))
        {
            _queuesByName.AddOrUpdate(
                newCommandsByQueue.Key,
                name =>
                {
                    var queue = new InMemoryWorkflowCommandQueue(_loggerFactory.CreateLogger<InMemoryWorkflowCommandQueue>(), name, _pipeline);

                    queue.Enqueue(newCommands);

                    return queue;
                },
                (name, queue) =>
                {
                    queue.Enqueue(newCommands);

                    return queue;
                });
        }
    }

    public void Dispose()
    {
        foreach(var queue in _queuesByName.Values)
        {
            queue.Dispose();
        }
    }
}
