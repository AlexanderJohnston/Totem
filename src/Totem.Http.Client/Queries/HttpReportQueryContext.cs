namespace Totem.Queries;

public sealed class HttpReportQueryContext<TQuery, TRow> : MessageContext, IHttpReportQueryContext<TQuery>
    where TQuery : IHttpReportQuery<TRow>
    where TRow : IReportRow
{
    internal HttpReportQueryContext(HttpReportQueryEnvelope envelope) : base(envelope) =>
        Query = (TQuery) envelope.Query;

    public new HttpReportQueryEnvelope Envelope => (HttpReportQueryEnvelope) base.Envelope;
    public TQuery Query { get; }
    public ReportQueryInfo QueryInfo => Envelope.QueryInfo;
    public Type QueryType => Envelope.QueryInfo.DeclaredType;
    public Id QueryId => Envelope.QueryId;
    public ReportRowInfo RowInfo => Envelope.RowInfo;
    public string? ETag => Envelope.ETag;
    public string? ResponseETag => Response?.Headers.ETag?.ToString();
    public HttpClientAdapterResponse? Response { get; set; }
    public TRow? Row { get; set; }

    IReportRow? IHttpReportQueryContext<TQuery>.Row
    {
        get => Row;
        set => Row = (TRow?) value;
    }
}
