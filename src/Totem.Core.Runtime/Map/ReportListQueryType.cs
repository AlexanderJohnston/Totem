namespace Totem.Map;

public sealed class ReportListQueryType : MessageType
{
    readonly Func<IReportListQuery> _createInstance;

    internal ReportListQueryType(ReportListQueryInfo info, ReportRowType row, Func<IReportListQuery> createInstance) : base(info)
    {
        Row = row;
        _createInstance = createInstance;
    }

    public new ReportListQueryInfo Info => (ReportListQueryInfo) base.Info;
    public ReportRowType Row { get; }

    public IReportListQuery CreateInstance() =>
        _createInstance();
}
