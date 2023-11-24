namespace Totem.Commands;

public sealed class HttpCommandContext<TCommand> : MessageContext, IHttpCommandContext<TCommand>
    where TCommand : IHttpCommand
{
    internal HttpCommandContext(HttpCommandEnvelope envelope) : base(envelope) =>
        Command = (TCommand) envelope.Command;

    public new HttpCommandEnvelope Envelope => (HttpCommandEnvelope) base.Envelope;
    public TCommand Command { get; }
    public CommandInfo CommandInfo => Envelope.CommandInfo;
    public Type CommandType => Envelope.CommandType;
    public Id CommandId => Envelope.CommandId;
    public HttpClientAdapterResponse? Response { get; set; }
}
