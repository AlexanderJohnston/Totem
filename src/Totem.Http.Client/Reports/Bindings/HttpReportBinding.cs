namespace Totem.Reports.Bindings;

internal sealed class HttpReportBinding<TRow> : ReportBindingBase, IHttpReportBinding<TRow>
    where TRow : IReportRow
{
    readonly IHttpReportBindingSource<TRow> _source;
    readonly ITotemHttpClient _httpClient;

    internal HttpReportBinding(IHttpReportBindingSource<TRow> source, ITotemHttpClient httpClient)
    {
        _source = source;
        _httpClient = httpClient;

        RunInitialLoadTask();
    }

    public string? ETag { get; private set; }
    public TRow? Row { get; private set; }
    [MemberNotNullWhen(true, nameof(Row))]
    public bool HasValue => Row is not null;

    protected override async Task LoadAsync(CancellationToken cancellationToken)
    {
        var query = new HttpReportQueryEnvelope(_source.CreateQuery(), ETag, _source.CorrelationId, _source.Principal);
        var context = await _httpClient.SendAsync(query, cancellationToken);

        context.ExpectNoErrors();

        ETag = context.ResponseETag;

        if(context.Row is not null)
        {
            Row = (TRow) context.Row;
        }
    }
}
