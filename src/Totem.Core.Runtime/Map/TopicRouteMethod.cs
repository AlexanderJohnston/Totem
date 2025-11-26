namespace Totem.Map;

public sealed class TopicRouteMethod : RuntimeMethod
{
    readonly Func<ICommandContext<ICommand>, Id> _call;

    internal TopicRouteMethod(MethodInfo info, CommandParameter parameter) : base(info, parameter)
    {
        // context => Route(<argument>)

        var contextParameter = Expression.Parameter(typeof(ICommandContext<ICommand>), "context");
        var call = Expression.Call(info, parameter.ToArgument(contextParameter));
        var lambda = Expression.Lambda<Func<ICommandContext<ICommand>, Id>>(call, contextParameter);

        _call = lambda.Compile();
    }

    public new CommandParameter Parameter => (CommandParameter) base.Parameter;

    internal TopicRoute Call(ICommandContext<ICommand> context) =>
        new(Parameter.Message.Topic, _call(context));
}
