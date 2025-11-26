namespace Totem.Reports.Bindings;

internal sealed class HttpReportListBinding<TRow> : ReportBindingBase, IHttpReportListBinding<TRow>
    where TRow : IReportRow
{
    readonly IHttpReportListBindingSource<TRow> _source;
    readonly ITotemHttpClient _httpClient;

    internal HttpReportListBinding(IHttpReportListBindingSource<TRow> source, ITotemHttpClient httpClient)
    {
        _source = source;
        _httpClient = httpClient;

        RunInitialLoadTask();
    }

    public string? ETag { get; private set; }
    public IReportList<TRow>? Rows { get; private set; }
    [MemberNotNullWhen(true, nameof(Rows))]
    public bool HasValue => Rows is not null;
    public bool HasRows => HasValue && Rows.Count > 0;

    protected override async Task LoadAsync(CancellationToken cancellationToken)
    {
        var query = new HttpReportListQueryEnvelope(_source.CreateQuery(), ETag, _source.CorrelationId, _source.Principal);
        var context = await _httpClient.SendAsync(query, cancellationToken);

        context.ExpectNoErrors();

        ETag = context.ResponseETag;

        if(context.Rows is not null)
        {
            Rows = new ReportList<TRow>(context.Rows.Cast<TRow>());
        }
    }
}
