using Totem.Core;
using Totem.Topics;

namespace Totem.External;
public class ExternalTopicStore : ITopicStore
{
    public Task CommitAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RollbackAsync(ITopicTransaction transaction, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ITopicTransaction> StartTransactionAsync(ITopicContext<ICommandMessage> context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
