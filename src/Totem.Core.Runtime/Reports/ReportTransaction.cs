namespace Totem.Reports;

public sealed class ReportTransaction : IReportTransaction
{
    readonly IReportStore _store;
    readonly CancellationToken _cancellationToken;

    public ReportTransaction(
        IReportStore store,
        IReportContext<IEvent> context,
        IReport report,
        TimelinePosition position,
        CancellationToken cancellationToken)
    {
        _store = store;
        Context = context;
        Report = report;
        Position = position;
        _cancellationToken = cancellationToken;
    }

    public IReportContext<IEvent> Context { get; }
    public IReport Report { get; }
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
