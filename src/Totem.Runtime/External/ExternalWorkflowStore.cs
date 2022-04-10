using Totem.Workflows;

namespace Totem.External;
public class ExternalWorkflowStore : IWorkflowStore
{
    public Task CommitAsync(IWorkflowTransaction transaction, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task RollbackAsync(IWorkflowTransaction transaction, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IWorkflowTransaction> StartTransactionAsync(IWorkflowContext<IEvent> context, CancellationToken cancellationToken) => throw new NotImplementedException();
}
