namespace Totem.Map.Builder;

internal sealed class ReportQueryBuilder : RuntimeTypeBuilder<ReportQueryType>
{
    ConstructorInfo? _defaultConstructor;
    ConstructorInfo? _idConstructor;
    bool _hasProperties;
    PropertyInfo? _idProperty;

    internal ReportQueryBuilder(Type declaredType) : base(declaredType)
    { }

    internal ReportQueryInfo Info { get; private set; } = null!;
    internal ReportRowBuilder Row { get; set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        if(!ReportQueryInfo.TryFrom(DeclaredType, out var info))
        {
            SetError(BuildErrors.MessageInfoNotFound);
            return;
        }

        Info = info;

        if(!map.ReportRows.Rows.TryGet(info.Row.DeclaredType, out var row))
        {
            SetError(BuildErrors.ReportRowNotFound, new { info.Row.DeclaredType });
            return;
        }

        _defaultConstructor = DeclaredType.GetConstructor(Type.EmptyTypes);
        _idConstructor = DeclaredType.GetConstructor(new[] { typeof(Id) });

        var properties = DeclaredType.GetProperties();

        _hasProperties = properties.Any();
        _idProperty = properties.FirstOrDefault(x => x.PropertyType == typeof(Id));

        Row = row;
        row.HasQuery = true;
    }

    internal override void Validate()
    {
        if(Row.HasError)
        {
            SetError(BuildErrors.ReportRowHasError);
            return;
        }

        if(Row.Report.IsSingleInstance)
        {
            if(_defaultConstructor is null)
            {
                SetError(BuildErrors.ReportQueryMissingDefaultConstructor);
                return;
            }

            if(_hasProperties)
            {
                SetError(BuildErrors.ReportQueryHasProperties);
                return;
            }
        }
        else
        {
            if(_idConstructor is null)
            {
                SetError(BuildErrors.ReportQueryMissingIdConstructor);
                return;
            }

            if(_idProperty is null)
            {
                SetError(BuildErrors.ReportQueryMissingIdProperty);
                return;
            }
        }
    }

    protected override ReportQueryType BuildValue()
    {
        var query = new ReportQueryType(
            Info,
            Row.Value,
            TryBuildIdProperty(),
            TryCompileCreateSingleInstance(),
            TryCompileCreateMultiInstance());

        Row.Value.ReportQueries.Add(query);

        return query;
    }

    ReportQueryTypeIdProperty? TryBuildIdProperty() =>
        _idProperty is null ? null : new(_idProperty);

    Func<IReportQuery>? TryCompileCreateSingleInstance()
    {
        if(_defaultConstructor is null)
        {
            return null;
        }

        // () => new _singleInstanceConstructor()

        var callConstructor = Expression.New(_defaultConstructor);
        var lambda = Expression.Lambda<Func<IReportQuery>>(callConstructor);

        return lambda.Compile();
    }

    Func<Id, IReportQuery>? TryCompileCreateMultiInstance()
    {
        if(_idConstructor is null)
        {
            return null;
        }

        // id => new _multiInstanceConstructor(id)

        var idParameter = Expression.Parameter(typeof(Id), "id");
        var callConstructor = Expression.New(_idConstructor, idParameter);
        var lambda = Expression.Lambda<Func<Id, IReportQuery>>(callConstructor, idParameter);

        return lambda.Compile();
    }
}
