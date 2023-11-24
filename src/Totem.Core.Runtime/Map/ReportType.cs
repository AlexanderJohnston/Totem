namespace Totem.Map;

public sealed class ReportType : ObserverType
{
    internal ReportType(Type declaredType, bool isSingleInstance, ReportRowType row) : base(declaredType, isSingleInstance) =>
        Row = row;

    public ReportRowType Row { get; }
}
