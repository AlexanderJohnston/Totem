namespace Totem;

public interface IReportListQueryContext<out TQuery> : IMessageContext
    where TQuery : IReportListQuery
{
    new ReportListQueryEnvelope Envelope { get; }
    TQuery Query { get; }
    ReportListQueryInfo QueryInfo { get; }
    ReportListQueryType QueryType { get; }
    Id QueryId { get; }
    ReportRowType RowType { get; }
    TimelineVersion? Checkpoint { get; }
    ReportListReadResult? Result { get; set; }
    [MemberNotNullWhen(true, nameof(Result))]
    bool HasResult { get; }
}
