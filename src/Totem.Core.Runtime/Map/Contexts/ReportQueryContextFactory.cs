namespace Totem.Map.Contexts;

internal sealed class ReportQueryContextFactory
{
    delegate IReportQueryContext<IReportQuery> CompiledFactory(ReportQueryEnvelope envelope);

    readonly ConcurrentDictionary<ReportQueryType, CompiledFactory> _factoriesByQueryType = new();
    readonly RuntimeMap _map;

    internal ReportQueryContextFactory(RuntimeMap map) =>
        _map = map;

    internal IReportQueryContext<IReportQuery> Create(ReportQueryEnvelope envelope)
    {
        if(!_map.ReportQueries.TryGet(envelope.QueryType, out var queryType))
            throw new ArgumentException($"Expected mapped row query of type {envelope.QueryType}", nameof(envelope));

        var factory = _factoriesByQueryType.GetOrAdd(queryType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(ReportQueryType queryType)
    {
        // envelope => new ReportQueryContext<TQuery>(envelope, queryType)

        var constructor = typeof(ReportQueryContext<>)
            .MakeGenericType(queryType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(ReportQueryEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter, Expression.Constant(queryType));
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}
