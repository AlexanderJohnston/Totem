namespace Totem.Map;

public sealed class ReportQueryTypeIdProperty : RuntimeProperty
{
    delegate Id CompiledGetValue(IReportQuery query);

    readonly CompiledGetValue _getValue;

    internal ReportQueryTypeIdProperty(PropertyInfo info) : base(info)
    {
        // query => ((TQuery) query).info

        var queryParameter = Expression.Parameter(typeof(IReportQuery), "query");
        var queryCast = Expression.Convert(queryParameter, info.DeclaringType!);
        var getValue = Expression.Property(queryCast, info);
        var lambda = Expression.Lambda<CompiledGetValue>(getValue, queryParameter);

        _getValue = lambda.Compile();
    }

    internal Id GetValue(IReportQuery query) =>
        _getValue(query) ?? throw new Exception($"Expected non-null report id from property '{this}'");
}
