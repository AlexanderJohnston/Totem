namespace Totem.Reports.Bindings;

internal sealed class WebReportListBinding<TRow> : ReportBindingBase, IWebReportListBinding<TRow>
    where TRow : IReportRow
{
    readonly WebReportBinder _binder;
    readonly IWebReportListBindingSource<TRow> _source;
    readonly ITotemTspClient _tspClient;
    IHttpReportListBinding<TRow> _httpBinding = null!;

    internal WebReportListBinding(WebReportBinder binder, IWebReportListBindingSource<TRow> source, ITotemTspClient tspClient)
    {
        _binder = binder;
        _source = source;
        _tspClient = tspClient;
        Address = source.CreateAddress();

        RunInitialLoadTask();
    }

    public SubscriptionAddress Address { get; }
    public string? ETag => _httpBinding.ETag;
    public IReportList<TRow>? Rows { get; private set; }
    [MemberNotNullWhen(true, nameof(Rows))]
    public bool HasValue => _httpBinding.HasValue;
    public bool HasRows => _httpBinding.HasRows;

    public override void NotifyChanged() =>
        _source.NotifyChanged();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _binder.RemoveBinding(Address);

        _httpBinding.Dispose();
    }

    protected override async Task LoadAsync(CancellationToken cancellationToken)
    {
        if(_httpBinding is not null)
        {
            await _httpBinding.ReloadAsync(cancellationToken);
            return;
        }

        _httpBinding = _source.BindHttpReportList();

        await _httpBinding.LoadTask;

        if(ETag is not null)
        {
            var subscription = new TspSubscriptionEnvelope(new WatchTspReport(ETag), Address);
            var context = await _tspClient.SendAsync(subscription, cancellationToken);

            context.ExpectNoErrors();
        }
    }
}
