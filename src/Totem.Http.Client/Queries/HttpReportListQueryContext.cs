namespace Totem.Queries;

public sealed class HttpReportListQueryContext<TQuery, TRow> : MessageContext, IHttpReportListQueryContext<TQuery>
    where TQuery : IHttpReportListQuery<TRow>
    where TRow : IReportRow
{
    internal HttpReportListQueryContext(HttpReportListQueryEnvelope envelope) : base(envelope) =>
        Query = (TQuery) envelope.Query;

    public new HttpReportListQueryEnvelope Envelope => (HttpReportListQueryEnvelope) base.Envelope;
    public TQuery Query { get; }
    public ReportListQueryInfo QueryInfo => Envelope.QueryInfo;
    public Type QueryType => Envelope.QueryInfo.DeclaredType;
    public Id QueryId => Envelope.QueryId;
    public ReportRowInfo RowInfo => Envelope.RowInfo;
    public string? ETag => Envelope.ETag;
    public string? ResponseETag => Response?.Headers.ETag?.ToString();
    public HttpClientAdapterResponse? Response { get; set; }
    public IReadOnlyList<TRow>? Rows { get; set; }

    IReadOnlyList<IReportRow>? IHttpReportListQueryContext<TQuery>.Rows
    {
        get => Rows?.Cast<IReportRow>().ToArray();
        set => Rows = (IReadOnlyList<TRow>?) value;
    }
}
