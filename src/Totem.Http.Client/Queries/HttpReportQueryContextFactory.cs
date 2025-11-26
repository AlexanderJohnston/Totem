namespace Totem.Queries;

internal sealed class HttpReportQueryContextFactory
{
    delegate IHttpReportQueryContext<IHttpReportQuery> CompiledFactory(HttpReportQueryEnvelope envelope);

    readonly ConcurrentDictionary<Type, CompiledFactory> _factoriesByQueryType = new();

    internal IHttpReportQueryContext<IHttpReportQuery> Create(HttpReportQueryEnvelope envelope)
    {
        var factory = _factoriesByQueryType.GetOrAdd(
            envelope.QueryType,
            queryType => CompileFactory(queryType, envelope.RowInfo.DeclaredType));

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(Type queryType, Type rowType)
    {
        // envelope => new HttpReportQueryContext<TQuery, TRow>(envelope)

        var constructor = typeof(HttpReportQueryContext<,>)
            .MakeGenericType(queryType, rowType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(HttpReportQueryEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter);
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}
