namespace Totem.InMemory.Reports;

public sealed class InMemoryReportStore : IReportStore, IReportReader
{
    readonly ConcurrentDictionary<ReportType, InMemoryReportStreamList> _listsByReport = new();
    readonly IServiceProvider _services;
    readonly IInMemoryReportBroker _broker;
    readonly TotemJsonFormat _jsonFormat;
    readonly RuntimeMap _map;

    public InMemoryReportStore(IServiceProvider services, IInMemoryReportBroker broker, TotemJsonFormat jsonFormat, RuntimeMap map)
    {
        _services = services;
        _broker = broker;
        _jsonFormat = jsonFormat;
        _map = map;
    }

    public Task<IReportTransaction> StartTransactionAsync(IReportContext<IEvent> context, CancellationToken cancellationToken)
    {
        var listStream = GetOrAddList(context.ReportType);

        lock(listStream)
        {
            var transaction = listStream.StartTransaction(context, cancellationToken);

            return Task.FromResult(transaction);
        }
    }

    public Task CommitAsync(IReportTransaction transaction, CancellationToken cancellationToken)
    {
        var list = GetOrAddList(transaction.Context.ReportType);

        lock(list)
        {
            var newVersion = list.Commit(transaction);

            _broker.PublishChanged(transaction.Context.ReportType, newVersion);

            return Task.CompletedTask;
        }
    }

    public Task RollbackAsync(IReportTransaction transaction, CancellationToken cancellationToken) =>
        // In-memory store does not keep state for open transactions
        Task.CompletedTask;

    public Task<ReportReadResult> ReadAsync(ReportType report, Id id, TimelineVersion? checkpoint, CancellationToken cancellationToken)
    {
        var list = GetOrAddList(report);

        lock(list)
        {
            var result = list.Read(id, checkpoint);

            return Task.FromResult(result);
        }
    }

    public Task<ReportListReadResult> ReadListAsync(ReportType report, TimelineVersion? checkpoint, CancellationToken cancellationToken)
    {
        var list = GetOrAddList(report);

        lock(list)
        {
            var result = list.ReadList(checkpoint);

            return Task.FromResult(result);
        }
    }

    InMemoryReportStreamList GetOrAddList(ReportType report) =>
        _listsByReport.GetOrAdd(report, _ => new(this, report, _services, _jsonFormat, _map));
}
