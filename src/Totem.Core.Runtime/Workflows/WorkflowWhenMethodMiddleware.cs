namespace Totem.Workflows;

public sealed class WorkflowWhenMethodMiddleware : IWorkflowMiddleware
{
    readonly IWorkflowStore _store;

    public WorkflowWhenMethodMiddleware(IWorkflowStore store) =>
        _store = store;

    public async Task InvokeAsync(IWorkflowContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var transaction = await _store.StartTransactionAsync(context, cancellationToken);

        try
        {
            context.Observation.CallWhenIfDefined(transaction.Workflow, context);

            context.ExpectNoErrors();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }

        await transaction.CommitAsync();
        await next();
    }
}
