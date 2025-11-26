namespace Totem.Map;

public sealed class ReportRowType : RuntimeType
{
    internal ReportRowType(Type declaredType, ReportRowInfo info, IReadOnlyList<ReportRowProperty> properties)
        : base(declaredType)
    {
        Info = info;
        Properties = properties;
        Create = Expression.Lambda<Func<IReportRow>>(Expression.New(declaredType)).Compile();
    }

    public ReportRowInfo Info { get; }
    public ReportType Report { get; internal set; } = null!;
    public IReadOnlyList<ReportRowProperty> Properties { get; }
    public RuntimeTypeCollection<ReportQueryType> ReportQueries { get; } = new();
    public RuntimeTypeCollection<ReportListQueryType> ReportListQueries { get; } = new();
    public Func<IReportRow> Create { get; }
}
