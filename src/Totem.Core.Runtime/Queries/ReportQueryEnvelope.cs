namespace Totem.Queries;

public sealed class ReportQueryEnvelope : MessageEnvelope
{
    public ReportQueryEnvelope(IReportQuery query, TimelineVersion? checkpoint, EnvelopeInfo info) : base(info)
    {
        Query = query;
        QueryInfo = ReportQueryInfo.From(query.GetType());
        Checkpoint = checkpoint;
    }

    public ReportQueryEnvelope(IReportQuery query, TimelineVersion? checkpoint = null, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(query, checkpoint, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public IReportQuery Query { get; }
    public ReportQueryInfo QueryInfo { get; }
    public Type QueryType => QueryInfo.DeclaredType;
    public Id QueryId => Info.MessageId;
    public ReportRowInfo RowInfo => QueryInfo.Row;
    public TimelineVersion? Checkpoint { get; }
}
