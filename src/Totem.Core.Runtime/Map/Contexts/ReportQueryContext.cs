namespace Totem.Map.Contexts;

internal sealed class ReportQueryContext<TQuery> : MessageContext, IReportQueryContext<TQuery>
    where TQuery : IReportQuery
{
    internal ReportQueryContext(ReportQueryEnvelope envelope, ReportQueryType queryType) : base(envelope)
    {
        Query = (TQuery) envelope.Query;
        QueryType = queryType;
    }

    public new ReportQueryEnvelope Envelope => (ReportQueryEnvelope) base.Envelope;
    public TQuery Query { get; }
    public ReportQueryInfo QueryInfo => Envelope.QueryInfo;
    public ReportQueryType QueryType { get; }
    public Id QueryId => Envelope.QueryId;
    public ReportRowType RowType => QueryType.Row;
    public TimelineVersion? Checkpoint => Envelope.Checkpoint;
    public ReportReadResult? Result { get; set; }
    public bool HasResult => Result is not null;
}
