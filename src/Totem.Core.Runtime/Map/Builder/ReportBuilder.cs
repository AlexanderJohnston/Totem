namespace Totem.Map.Builder;

internal sealed class ReportBuilder : ObserverBuilder<ReportType>
{
    internal ReportBuilder(Type declaredType) : base(declaredType)
    { }

    internal ReportRowBuilder Row { get; private set; } = null!;

    protected override Type ExtendedContextType => typeof(IReportContext<>);

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var rowType = DeclaredType.GetImplementedInterfaceGenericArguments(typeof(IReport<>)).SingleOrDefault();

        if(rowType is null)
        {
            SetError(BuildErrors.ReportRowNotSpecified);
            return;
        }

        if(!map.ReportRows.Rows.TryGet(rowType, out var row))
        {
            SetError(BuildErrors.ReportRowNotFound, new { rowType });
            return;
        }

        if(row.Report is not null)
        {
            SetError(BuildErrors.ReportRowDuplicated);
            return;
        }

        Row = row;
        row.Report = this;

        base.Reflect(map);
    }

    internal override void Validate()
    {
        if(Row.HasError)
        {
            SetError(BuildErrors.ReportRowHasError);
            return;
        }

        base.Validate();
    }

    protected override ReportType BuildValue()
    {
        var report = new ReportType(DeclaredType, IsSingleInstance, Row.Value);

        Row.Value.Report = report;

        foreach(var builder in Observations)
        {
            if(builder.HasError)
            {
                continue;
            }

            var observation = builder.Build();

            report.Observations.Add(observation);

            observation.Observer = report;
            observation.Event.ReportObservations.Add(observation);
        }

        return report;
    }
}
