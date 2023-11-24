namespace Totem.Workflows;

public sealed class WorkflowTransaction : IWorkflowTransaction
{
    readonly IWorkflowStore _store;
    readonly CancellationToken _cancellationToken;

    public WorkflowTransaction(
        IWorkflowStore store,
        IWorkflowContext<IEvent> context,
        IWorkflow workflow,
        TimelinePosition position,
        CancellationToken cancellationToken)
    {
        _store = store;
        Context = context;
        Workflow = workflow;
        Position = position;
        _cancellationToken = cancellationToken;
    }

    public IWorkflowContext<IEvent> Context { get; }
    public IWorkflow Workflow { get; }
    public TimelinePosition Position { get; }

    public async Task CommitAsync()
    {
        if(!_cancellationToken.IsCancellationRequested)
        {
            await _store.CommitAsync(this, _cancellationToken);
        }
    }

    public Task RollbackAsync() =>
        _store.RollbackAsync(this, _cancellationToken);
}
