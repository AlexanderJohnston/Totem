namespace Totem.Queries;

internal sealed class HttpReportListQueryContextFactory
{
    delegate IHttpReportListQueryContext<IHttpReportListQuery> CompiledFactory(HttpReportListQueryEnvelope envelope);

    readonly ConcurrentDictionary<Type, CompiledFactory> _factoriesByQueryType = new();

    internal IHttpReportListQueryContext<IHttpReportListQuery> Create(HttpReportListQueryEnvelope envelope)
    {
        var factory = _factoriesByQueryType.GetOrAdd(
            envelope.QueryType,
            queryType => CompileFactory(queryType, envelope.RowInfo.DeclaredType));

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(Type queryType, Type rowType)
    {
        // envelope => new HttpReportListQueryContext<TQuery, TRow>(envelope)

        var constructor = typeof(HttpReportListQueryContext<,>)
            .MakeGenericType(queryType, rowType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(HttpReportListQueryEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter);
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}
