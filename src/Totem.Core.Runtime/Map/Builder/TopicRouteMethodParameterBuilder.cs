namespace Totem.Map.Builder;

internal sealed class TopicRouteMethodParameterBuilder : RuntimeMethodParameterBuilder<CommandParameter>
{
    internal TopicRouteMethodParameterBuilder(ParameterInfo parameter) : base(parameter)
    { }

    internal CommandBuilder Command { get; private set; } = null!;
    internal bool HasContext { get; private set; }

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var type = Info.ParameterType;
        var isCommand = typeof(ICommand).IsAssignableFrom(type);
        var commandType = isCommand ? type : type.GetImplementedInterfaceGenericArguments(typeof(ICommandContext<>)).SingleOrDefault();

        if(commandType is null)
        {
            SetError(BuildErrors.RuntimeMethodParameterNotMessageOrContext);
            return;
        }

        if(!map.Messages.Commands.TryGet(commandType, out var command))
        {
            SetError(BuildErrors.CommandNotFound);
            return;
        }

        Command = command;
        HasContext = !isCommand;
    }

    protected override CommandParameter BuildValue() =>
        new(Info, Command.Value, HasContext);
}
