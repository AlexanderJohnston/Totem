namespace Totem.Map.Contexts;

internal sealed class ReportListQueryContext<TQuery> : MessageContext, IReportListQueryContext<TQuery>
    where TQuery : IReportListQuery
{
    internal ReportListQueryContext(ReportListQueryEnvelope envelope, ReportListQueryType queryType) : base(envelope)
    {
        Query = (TQuery) envelope.Query;
        QueryType = queryType;
    }

    public new ReportListQueryEnvelope Envelope => (ReportListQueryEnvelope) base.Envelope;
    public TQuery Query { get; }
    public ReportListQueryType QueryType { get; }
    public ReportListQueryInfo QueryInfo => Envelope.QueryInfo;
    public Id QueryId => Envelope.QueryId;
    public ReportRowType RowType => QueryType.Row;
    public TimelineVersion? Checkpoint => Envelope.Checkpoint;
    public ReportListReadResult? Result { get; set; }
    public bool HasResult => Result is not null;
}
