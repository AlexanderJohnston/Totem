namespace Totem.Map.Contexts;

internal sealed class CommandContextFactory
{
    delegate ICommandContext<ICommand> CompiledFactory(CommandEnvelope envelope);

    readonly ConcurrentDictionary<CommandType, CompiledFactory> _factoriesByCommandType = new();
    readonly RuntimeMap _map;

    internal CommandContextFactory(RuntimeMap map) =>
        _map = map;

    internal ICommandContext<ICommand> Create(CommandEnvelope envelope)
    {
        if(!_map.Commands.TryGet(envelope.CommandType, out var commandType))
            throw new ArgumentException($"Expected mapped command of type {envelope.CommandType}", nameof(envelope));

        var factory = _factoriesByCommandType.GetOrAdd(commandType, CompileFactory);

        return factory(envelope);
    }

    static CompiledFactory CompileFactory(CommandType commandType)
    {
        // envelope => new CommandContext<TCommand>(envelope, commandType)

        var constructor = typeof(CommandContext<>)
            .MakeGenericType(commandType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var envelopeParameter = Expression.Parameter(typeof(CommandEnvelope), "envelope");
        var constructorCall = Expression.New(constructor, envelopeParameter, Expression.Constant(commandType));
        var lambda = Expression.Lambda<CompiledFactory>(constructorCall, envelopeParameter);

        return lambda.Compile();
    }
}
