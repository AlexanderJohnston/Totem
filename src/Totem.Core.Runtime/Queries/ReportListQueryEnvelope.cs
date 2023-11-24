namespace Totem.Queries;

public sealed class ReportListQueryEnvelope : MessageEnvelope
{
    public ReportListQueryEnvelope(IReportListQuery query, TimelineVersion? checkpoint, EnvelopeInfo info) : base(info)
    {
        Query = query;
        QueryInfo = ReportListQueryInfo.From(query.GetType());
        Checkpoint = checkpoint;
    }

    public ReportListQueryEnvelope(IReportListQuery query, TimelineVersion? checkpoint = null, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(query, checkpoint, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public IReportListQuery Query { get; }
    public ReportListQueryInfo QueryInfo { get; }
    public Type QueryType => QueryInfo.DeclaredType;
    public Id QueryId => Info.MessageId;
    public ReportRowInfo RowInfo => QueryInfo.Row;
    public TimelineVersion? Checkpoint { get; }
}
