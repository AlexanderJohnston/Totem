namespace Totem.Commands;

internal sealed class HttpCommandContextFactory
{
    delegate IHttpCommandContext<IHttpCommand> CompiledFactory(HttpCommandEnvelope envelope);

    readonly ConcurrentDictionary<Type, CompiledFactory> _factoriesByCommandType = new();

    internal IHttpCommandContext<IHttpCommand> Create(HttpCommandEnvelope envelope)
    {
        var factory = _factoriesByCommandType.GetOrAdd(envelope.CommandType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(Type commandType)
    {
        // envelope => new HttpCommandContext<TCommand>(envelope)

        var constructor = typeof(HttpCommandContext<>)
            .MakeGenericType(commandType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(HttpCommandEnvelope), "envelope");
        var callConstructor = Expression.New(constructor, envelopeParameter);
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, envelopeParameter);

        return lambda.Compile();
    }
}
