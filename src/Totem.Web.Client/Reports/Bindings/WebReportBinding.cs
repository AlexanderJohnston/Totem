namespace Totem.Reports.Bindings;

internal sealed class WebReportBinding<TRow> : ReportBindingBase, IWebReportBinding<TRow>
    where TRow : IReportRow
{
    readonly WebReportBinder _binder;
    readonly IWebReportBindingSource<TRow> _source;
    readonly ITotemTspClient _tspClient;
    IHttpReportBinding<TRow> _httpBinding = null!;

    internal WebReportBinding(WebReportBinder binder, IWebReportBindingSource<TRow> source, ITotemTspClient tspClient)
    {
        _binder = binder;
        _source = source;
        _tspClient = tspClient;
        Address = source.CreateAddress();

        RunInitialLoadTask();
    }

    public SubscriptionAddress Address { get; }
    public string? ETag => _httpBinding.ETag;
    public TRow? Row => _httpBinding.Row;
    [MemberNotNullWhen(true, nameof(Row))]
    public bool HasValue => _httpBinding.HasValue;

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

        _httpBinding = _source.BindHttpReport();

        await _httpBinding.LoadTask;

        if(ETag is not null)
        {
            var subscription = new TspSubscriptionEnvelope(new WatchTspReport(ETag), Address);
            var context = await _tspClient.SendAsync(subscription, cancellationToken);

            context.ExpectNoErrors();
        }
    }
}
