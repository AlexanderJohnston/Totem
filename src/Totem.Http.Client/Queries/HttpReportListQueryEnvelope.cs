namespace Totem.Queries;

public sealed class HttpReportListQueryEnvelope : MessageEnvelope
{
    public HttpReportListQueryEnvelope(IHttpReportListQuery query, string? etag, EnvelopeInfo info) : base(info)
    {
        Query = query;
        QueryInfo = ReportListQueryInfo.From(query.GetType());
        ETag = etag;
    }

    public HttpReportListQueryEnvelope(IHttpReportListQuery query, string? etag, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(query, etag, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public IHttpReportListQuery Query { get; }
    public ReportListQueryInfo QueryInfo { get; }
    public Type QueryType => QueryInfo.DeclaredType;
    public Id QueryId => Info.MessageId;
    public ReportRowInfo RowInfo => QueryInfo.Row;
    public string? ETag { get; }
}
