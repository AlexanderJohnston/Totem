namespace Totem.Commands;

public sealed class CommandEnvelope : MessageEnvelope
{
    public CommandEnvelope(ICommand command, EnvelopeInfo info) : base(info)
    {
        Command = command;
        CommandInfo = CommandInfo.From(command.GetType());
    }

    public CommandEnvelope(ICommand command, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(command, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public ICommand Command { get; }
    public CommandInfo CommandInfo { get; }
    public Type CommandType => CommandInfo.DeclaredType;
    public Id CommandId => Info.MessageId;
}
