namespace Totem.Map.Builder;

internal sealed class ReportListQueryBuilder : RuntimeTypeBuilder<ReportListQueryType>
{
    ConstructorInfo _defaultConstructor = null!;

    internal ReportListQueryBuilder(Type declaredType) : base(declaredType)
    { }

    internal ReportListQueryInfo Info { get; private set; } = null!;
    internal ReportRowBuilder Row { get; set; } = null!;
    
    internal override void Reflect(RuntimeMapBuilder map)
    {
        if(!ReportListQueryInfo.TryFrom(DeclaredType, out var info))
        {
            SetError(BuildErrors.MessageInfoNotFound);
            return;
        }

        Info = info;

        if(!map.ReportRows.Rows.TryGet(info.Row.DeclaredType, out var row))
        {
            SetError(BuildErrors.ReportRowNotFound, new { info.Row });
            return;
        }

        if(DeclaredType.GetProperties(BindingFlags.Instance).Any())
        {
            SetError(BuildErrors.ReportListQueryHasProperties);
            return;
        }

        var defaultConstructor = DeclaredType.GetConstructor(Type.EmptyTypes);

        if(defaultConstructor is null)
        {
            SetError(BuildErrors.ReportListQueryMissingDefaultConstructor);
            return;
        }

        _defaultConstructor = defaultConstructor;

        Row = row;
        Row.HasQuery = true;
    }

    internal override void Validate()
    {
        if(Row.HasError)
        {
            SetError(BuildErrors.ReportRowHasError);
        }
    }

    protected override ReportListQueryType BuildValue()
    {
        var query = new ReportListQueryType(Info, Row.Value, CompileCreateInstance());

        Row.Value.ReportListQueries.Add(query);

        return query;
    }

    Func<IReportListQuery> CompileCreateInstance()
    {
        // () => new _defaultConstructor()

        var callConstructor = Expression.New(_defaultConstructor);
        var lambda = Expression.Lambda<Func<IReportListQuery>>(callConstructor);

        return lambda.Compile();
    }
}
