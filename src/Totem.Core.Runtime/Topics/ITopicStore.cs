namespace Totem.Topics;

public interface ITopicStore
{
    Task<ITopicTransaction> StartTransactionAsync(ITopicContext<ICommand> context, CancellationToken cancellationToken);
    Task CommitAsync(ITopicTransaction transaction, CancellationToken cancellationToken);
    Task RollbackAsync(ITopicTransaction transaction, CancellationToken cancellationToken);
}
