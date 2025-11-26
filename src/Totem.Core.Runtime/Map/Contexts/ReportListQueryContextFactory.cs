namespace Totem.Map.Contexts;

internal sealed class ReportListQueryContextFactory
{
    delegate IReportListQueryContext<IReportListQuery> CompiledFactory(ReportListQueryEnvelope envelope);

    readonly ConcurrentDictionary<ReportListQueryType, CompiledFactory> _factoriesByQueryType = new();
    readonly RuntimeMap _map;

    internal ReportListQueryContextFactory(RuntimeMap map) =>
        _map = map;

    internal IReportListQueryContext<IReportListQuery> Create(ReportListQueryEnvelope envelope)
    {
        if(!_map.ReportListQueries.TryGet(envelope.QueryType, out var queryType))
            throw new ArgumentException($"Expected mapped list query of type {envelope.QueryType}", nameof(envelope));

        var factory = _factoriesByQueryType.GetOrAdd(queryType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(ReportListQueryType queryType)
    {
        // envelope => new ReportListQueryContext<TQuery>(envelope, queryType)

        var constructor = typeof(ReportListQueryContext<>)
            .MakeGenericType(queryType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(ReportListQueryEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter, Expression.Constant(queryType));
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}
