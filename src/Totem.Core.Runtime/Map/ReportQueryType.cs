namespace Totem.Map;

public sealed class ReportQueryType : MessageType
{
    readonly Func<IReportQuery>? _createSingleInstance;
    readonly Func<Id, IReportQuery>? _createInstance;

    internal ReportQueryType(
        ReportQueryInfo info,
        ReportRowType row,
        ReportQueryTypeIdProperty? idProperty,
        Func<IReportQuery>? createSingleInstance,
        Func<Id, IReportQuery>? createInstance) : base(info)
    {
        Row = row;
        IdProperty = idProperty;
        _createSingleInstance = createSingleInstance;
        _createInstance = createInstance;
    }

    public new ReportQueryInfo Info => (ReportQueryInfo) base.Info;
    public ReportRowType Row { get; }
    public ReportQueryTypeIdProperty? IdProperty { get; }

    public IReportQuery CreateSingleInstance()
    {
        if(_createSingleInstance is null)
            throw new Exception($"Expected report to be single-instance: {Row.Report}");

        return _createSingleInstance();
    }

    public IReportQuery CreateInstance(Id id)
    {
        if(_createInstance is null)
            throw new Exception($"Expected report to be multi-instance: {Row.Report}");

        return _createInstance(id);
    }

    internal Id ResolveId(IReportQuery query) =>
        IdProperty?.GetValue(query) ?? Row.Report.SingleInstanceId!;
}
