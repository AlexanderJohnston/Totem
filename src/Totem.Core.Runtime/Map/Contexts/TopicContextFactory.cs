namespace Totem.Map.Contexts;

internal sealed class TopicContextFactory
{
    delegate ITopicContext<ICommand> CompiledFactory(ICommandContext<ICommand> commandContext, TopicRoute route);

    readonly ConcurrentDictionary<CommandType, CompiledFactory> _factoriesByCommandType = new();

    internal ITopicContext<ICommand> Create(ICommandContext<ICommand> commandContext, TopicRoute route)
    {
        var factory = _factoriesByCommandType.GetOrAdd(commandContext.CommandType, CompileFactory);

        return factory(commandContext, route);
    }

    static CompiledFactory CompileFactory(CommandType commandType)
    {
        // (commandContext, route) => new TopicContext<TCommand>((ICommandContext<TCommand>) commandContext, route)

        var constructor = typeof(TopicContext<>)
            .MakeGenericType(commandType.DeclaredType)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

        var commandContextParameter = Expression.Parameter(typeof(ICommandContext<ICommand>), "commandContext");
        var routeParameter = Expression.Parameter(typeof(TopicRoute), "route");
        var commandContextCast = Expression.Convert(commandContextParameter, typeof(ICommandContext<>).MakeGenericType(commandType.DeclaredType));
        var callConstructor = Expression.New(constructor, commandContextCast, routeParameter);
        var lambda = Expression.Lambda<CompiledFactory>(callConstructor, commandContextParameter, routeParameter);

        return lambda.Compile();
    }
}
