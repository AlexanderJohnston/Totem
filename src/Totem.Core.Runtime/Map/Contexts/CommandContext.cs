namespace Totem.Map.Contexts;

internal sealed class CommandContext<TCommand> : MessageContext, ICommandContext<TCommand>
    where TCommand : ICommand
{
    internal CommandContext(CommandEnvelope envelope, CommandType commandType) : base(envelope)
    {
        Command = (TCommand) envelope.Command;
        CommandType = commandType;
    }

    public new CommandEnvelope Envelope => (CommandEnvelope) base.Envelope;
    public TCommand Command { get; }
    public CommandInfo CommandInfo => Envelope.CommandInfo;
    public CommandType CommandType { get; }
    public Id CommandId => Envelope.CommandId;
}
