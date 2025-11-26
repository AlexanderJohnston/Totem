namespace Totem.Map.Builder;

internal sealed class ReportRowBuilder : RuntimeTypeBuilder<ReportRowType>
{
    internal ReportRowBuilder(Type declaredType) : base(declaredType)
    { }

    internal ReportRowInfo Info { get; private set; } = null!;
    internal ReportBuilder Report { get; set; } = null!;
    internal bool HasQuery { get; set; }

    internal override void Reflect(RuntimeMapBuilder map)
    {
        if(!ReportRowInfo.TryFrom(DeclaredType, out var info))
        {
            SetError(BuildErrors.ReportRowInfoNotFound);
            return;
        }

        Info = info;
    }

    internal override void Validate()
    {
        if(Report is null)
        {
            SetError(BuildErrors.ReportNotFound);
            return;
        }

        if(!HasQuery)
        {
            SetError(BuildErrors.ReportRowNotQueried);
        }
    }

    protected override ReportRowType BuildValue()
    {
        var properties = DeclaredType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => new ReportRowProperty(x))
            .ToList();

        return new(DeclaredType, Info, properties);
    }
}
