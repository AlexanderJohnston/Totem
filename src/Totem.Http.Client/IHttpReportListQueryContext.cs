namespace Totem;

public interface IHttpReportListQueryContext<out TQuery> : IMessageContext
    where TQuery : IHttpReportListQuery
{
    new HttpReportListQueryEnvelope Envelope { get; }
    TQuery Query { get; }
    ReportListQueryInfo QueryInfo { get; }
    Type QueryType { get; }
    Id QueryId { get; }
    ReportRowInfo RowInfo { get; }
    string? ETag { get; }
    string? ResponseETag { get; }
    HttpClientAdapterResponse? Response { get; set; }
    IReadOnlyList<IReportRow>? Rows { get; set; }
}

public interface IHttpReportListQueryContext<out TQuery, TRow> : IHttpReportListQueryContext<TQuery>
    where TQuery : IHttpReportListQuery<TRow>
    where TRow : IReportRow
{
    new IReadOnlyList<TRow>? Rows { get; set; }
}
