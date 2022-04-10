using Totem.Reports;

namespace Totem.External;
public class ExternalReportStore : IReportStore
{
    public Task CommitAsync(IReportTransaction transaction, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task RollbackAsync(IReportTransaction transaction, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReportTransaction> StartTransactionAsync(IReportContext<IEvent> context, CancellationToken cancellationToken) => throw new NotImplementedException();
}
