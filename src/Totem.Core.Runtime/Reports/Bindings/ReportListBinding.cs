namespace Totem.Reports.Bindings;

internal sealed class ReportListBinding<TRow> : ReportBindingBase, IReportListBinding<TRow>
    where TRow : IReportRow
{
    readonly ReportBinder _binder;
    readonly IReportListBindingSource<TRow> _source;
    readonly IReportListQueryPipeline _queryPipeline;

    internal ReportListBinding(ReportBinder binder, IReportListBindingSource<TRow> source, IReportListQueryPipeline queryPipeline)
    {
        _binder = binder;
        _source = source;
        _queryPipeline = queryPipeline;
        Address = source.CreateAddress();

        RunInitialLoadTask();
    }

    public SubscriptionAddress Address { get; }
    public IReportList<TRow>? Rows { get; private set; }
    public TimelineVersion? Version { get; private set; }
    [MemberNotNullWhen(true, nameof(Rows), nameof(Version))]
    public bool HasValue => Rows is not null;
    public bool HasRows => HasValue && Rows.Count > 0;

    public override void NotifyChanged() =>
        _source.NotifyChanged();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _binder.RemoveBinding(Address);
    }

    protected override async Task LoadAsync(CancellationToken cancellationToken)
    {
        var query = new ReportListQueryEnvelope(_source.CreateQuery(), Version, _source.CorrelationId, _source.Principal);
        var context = await _queryPipeline.RunAsync(query, cancellationToken);
        var result = context.Result;

        context.ExpectNoErrors();

        if(result is null)
            throw new Exception("Expected pipeline to set query result or add an error");

        Version = result.Version;

        if(result.Rows is not null)
        {
            Rows = new ReportList<TRow>(result.Rows.Cast<TRow>());
        }
    }
}
