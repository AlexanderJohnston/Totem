namespace Totem.Queries;

public sealed class ReportListQueryReaderMiddleware : IReportListQueryMiddleware
{
    readonly IReportReader _reader;

    public ReportListQueryReaderMiddleware(IReportReader reader) =>
        _reader = reader;

    public async Task InvokeAsync(IReportListQueryContext<IReportListQuery> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var report = context.QueryType.Row.Report;
        var checkpoint = context.Checkpoint;

        context.Result = await _reader.ReadListAsync(report, checkpoint, cancellationToken);

        await next();
    }
}
