namespace Totem;

public interface IReportQueryContext<out TQuery> : IMessageContext
    where TQuery : IReportQuery
{
    new ReportQueryEnvelope Envelope { get; }
    TQuery Query { get; }
    ReportQueryInfo QueryInfo { get; }
    ReportQueryType QueryType { get; }
    Id QueryId { get; }
    ReportRowType RowType { get; }
    TimelineVersion? Checkpoint { get; }
    ReportReadResult? Result { get; set; }
    [MemberNotNullWhen(true, nameof(Result))]
    bool HasResult { get; }
}
