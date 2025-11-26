namespace Totem.Queries;

public sealed class HttpReportQueryEnvelope : MessageEnvelope
{
    public HttpReportQueryEnvelope(IHttpReportQuery query, string? etag, EnvelopeInfo info) : base(info)
    {
        Query = query;
        QueryInfo = ReportQueryInfo.From(query.GetType());
        ETag = etag;
    }

    public HttpReportQueryEnvelope(IHttpReportQuery query, string? etag, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(query, etag, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public IHttpReportQuery Query { get; }
    public ReportQueryInfo QueryInfo { get; }
    public Type QueryType => QueryInfo.DeclaredType;
    public Id QueryId => Info.MessageId;
    public ReportRowInfo RowInfo => QueryInfo.Row;
    public string? ETag { get; }
}
