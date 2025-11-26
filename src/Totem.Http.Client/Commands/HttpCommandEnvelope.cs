namespace Totem.Commands;

public sealed class HttpCommandEnvelope : MessageEnvelope
{
    public HttpCommandEnvelope(IHttpCommand command, EnvelopeInfo info) : base(info)
    {
        Command = command;
        CommandInfo = CommandInfo.From(command.GetType());
    }

    public HttpCommandEnvelope(IHttpCommand command, Id? correlationId = null, ClaimsPrincipal? principal = null)
        : this(command, new EnvelopeInfo(Id.NewId(), correlationId, principal))
    { }

    public IHttpCommand Command { get; }
    public CommandInfo CommandInfo { get; }
    public Type CommandType => CommandInfo.DeclaredType;
    public Id CommandId => Info.MessageId;
}
