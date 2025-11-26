namespace Totem.Reports.Bindings;

internal sealed class ReportBinding<TRow> : ReportBindingBase, IReportBinding<TRow>
    where TRow : IReportRow
{
    readonly ReportBinder _binder;
    readonly IReportBindingSource<TRow> _source;
    readonly IReportQueryPipeline _queryPipeline;

    internal ReportBinding(ReportBinder binder, IReportBindingSource<TRow> source, IReportQueryPipeline queryPipeline)
    {
        _binder = binder;
        _source = source;
        _queryPipeline = queryPipeline;
        Address = source.CreateAddress();

        RunInitialLoadTask();
    }

    public SubscriptionAddress Address { get; }
    public TRow? Row { get; private set; }
    public TimelineVersion? Version { get; private set; }
    [MemberNotNullWhen(true, nameof(Version), nameof(Row))]
    public bool HasValue => Row is not null;

    public override void NotifyChanged() =>
        _source.NotifyChanged();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _binder.RemoveBinding(Address);
    }

    protected override async Task LoadAsync(CancellationToken cancellationToken)
    {
        var query = new ReportQueryEnvelope(_source.CreateQuery(), Version, _source.CorrelationId, _source.Principal);
        var context = await _queryPipeline.RunAsync(query, cancellationToken);
        var result = context.Result;

        context.ExpectNoErrors();

        if(result is null)
            throw new Exception("Expected pipeline to set query result or add an error");

        Version = result.Version;

        if(result.Row is not null)
            Row = (TRow) result.Row;
    }
}
