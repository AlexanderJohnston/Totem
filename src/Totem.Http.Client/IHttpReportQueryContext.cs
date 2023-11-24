namespace Totem;

public interface IHttpReportQueryContext<out TQuery> : IMessageContext
    where TQuery : IHttpReportQuery
{
    new HttpReportQueryEnvelope Envelope { get; }
    ReportQueryInfo QueryInfo { get; }
    TQuery Query { get; }
    Type QueryType { get; }
    Id QueryId { get; }
    ReportRowInfo RowInfo { get; }
    string? ETag { get; }
    string? ResponseETag { get; }
    HttpClientAdapterResponse? Response { get; set; }
    IReportRow? Row { get; set; }
}

public interface IHttpReportQueryContext<out TQuery, TRow> : IHttpReportQueryContext<TQuery>
    where TQuery : IHttpReportQuery<TRow>
    where TRow : IReportRow
{
    new TRow? Row { get; set; }
}
