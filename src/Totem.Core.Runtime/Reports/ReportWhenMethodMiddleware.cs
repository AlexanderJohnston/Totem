namespace Totem.Reports;

public sealed class ReportWhenMethodMiddleware : IReportMiddleware
{
    readonly IReportStore _store;

    public ReportWhenMethodMiddleware(IReportStore store) =>
        _store = store;

    public async Task InvokeAsync(IReportContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var transaction = await _store.StartTransactionAsync(context, cancellationToken);

        try
        {
            context.Observation.CallWhenIfDefined(transaction.Report, context);

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
