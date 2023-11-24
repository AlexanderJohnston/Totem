namespace Totem.InMemory;

public sealed class InMemoryWorkflowStore : IWorkflowStore
{
    readonly ConcurrentDictionary<TimelineKey, InMemoryWorkflowStream> _streamsByKey = new();
    readonly IServiceProvider _services;
    readonly IInMemoryWorkflowCommandBus _queue;
    readonly RuntimeMap _map;

    public InMemoryWorkflowStore(IServiceProvider services, IInMemoryWorkflowCommandBus queue, RuntimeMap map)
    {
        _services = services;
        _queue = queue;
        _map = map;
    }

    public Task<IWorkflowTransaction> StartTransactionAsync(IWorkflowContext<IEvent> context, CancellationToken cancellationToken)
    {
        var workflow = (IWorkflow) _services.GetRequiredService(context.WorkflowType.DeclaredType);

        if(workflow is ITimelineInit init)
        {
            init.TimelineId = context.WorkflowId;
        }

        var version = _streamsByKey.TryGetValue(context.WorkflowKey, out var stream)
            ? stream.Load(workflow)
            : TimelinePosition.Start;

        context.WorkflowType.CallGivenIfDefined(workflow, context);

        var transaction = new WorkflowTransaction(this, context, workflow, version, cancellationToken);

        return Task.FromResult<IWorkflowTransaction>(transaction);
    }

    public Task CommitAsync(IWorkflowTransaction transaction, CancellationToken cancellationToken)
    {
        _streamsByKey.AddOrUpdate(
            transaction.Context.WorkflowKey,
            key => AddStream(transaction),
            (_, stream) => UpdateStream(stream, transaction));

        return Task.CompletedTask;
    }

    public Task RollbackAsync(IWorkflowTransaction transaction, CancellationToken cancellationToken) =>
        // In-memory store does not keep state for open transactions
        Task.CompletedTask;

    InMemoryWorkflowStream AddStream(IWorkflowTransaction transaction)
    {
        var stream = new InMemoryWorkflowStream(transaction.Context.WorkflowType, _map);

        return UpdateStream(stream, transaction);
    }

    InMemoryWorkflowStream UpdateStream(InMemoryWorkflowStream stream, IWorkflowTransaction transaction)
    {
        var newCommands = stream.Commit(transaction);

        _queue.Publish(newCommands);

        return stream;
    }
}
