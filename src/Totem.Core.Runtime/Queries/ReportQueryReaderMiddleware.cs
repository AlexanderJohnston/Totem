namespace Totem.Queries;

public sealed class ReportQueryReaderMiddleware : IReportQueryMiddleware
{
    readonly IReportReader _reader;

    public ReportQueryReaderMiddleware(IReportReader reader) =>
        _reader = reader;

    public async Task InvokeAsync(IReportQueryContext<IReportQuery> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var report = context.QueryType.Row.Report;
        var id = context.QueryType.ResolveId(context.Query);
        var checkpoint = context.Checkpoint;
        
        context.Result = await _reader.ReadAsync(report, id, checkpoint, cancellationToken);

        await next();
    }
}
