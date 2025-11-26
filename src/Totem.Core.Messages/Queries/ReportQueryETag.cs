namespace Totem.Queries;

public sealed class ReportQueryETag
{
    public ReportQueryETag(ReportQueryScope scope, ReportRowInfo rowInfo, TimelineVersion checkpoint)
    {
        Scope = scope;
        RowInfo = rowInfo;
        Checkpoint = checkpoint;
    }

    public ReportQueryScope Scope { get; }
    public ReportRowInfo RowInfo { get; }
    public TimelineVersion Checkpoint { get; }

    public static ReportQueryETag Row(ReportRowInfo rowInfo, TimelineVersion checkpoint) =>
        new(ReportQueryScope.Row, rowInfo, checkpoint);

    public static ReportQueryETag List(ReportRowInfo rowInfo, TimelineVersion checkpoint) =>
        new(ReportQueryScope.List, rowInfo, checkpoint);

    public static ReportQueryETag EmptyList(ReportRowInfo rowInfo) =>
        List(rowInfo, TimelineVersion.EmptyList);
}
