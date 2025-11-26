namespace Totem.Map.Contexts;

internal sealed class TopicContext<TCommand> : MessageContext, ITopicContext<TCommand>
    where TCommand : ICommand
{
    readonly ICommandContext<TCommand> _commandContext;

    internal TopicContext(ICommandContext<TCommand> commandContext, TopicRoute route) : base(commandContext.Envelope)
    {
        _commandContext = commandContext;

        TopicKey = new(route.TopicType.DeclaredType, route.TopicId);
        TopicType = route.TopicType;
        TopicId = route.TopicId;
    }

    public new CommandEnvelope Envelope => (CommandEnvelope) base.Envelope;
    public TCommand Command => _commandContext.Command;
    public CommandInfo CommandInfo => _commandContext.CommandInfo;
    public CommandType CommandType => _commandContext.CommandType;
    public Id CommandId => _commandContext.CommandId;
    public TimelineKey TopicKey { get; }
    public TopicType TopicType { get; }
    public Id TopicId { get; }

    public override string ToString() =>
        $"{_commandContext} => {TopicKey}";
}
