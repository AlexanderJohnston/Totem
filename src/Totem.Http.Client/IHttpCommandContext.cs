namespace Totem;

public interface IHttpCommandContext<out TCommand> : IMessageContext
    where TCommand : IHttpCommand
{
    new HttpCommandEnvelope Envelope { get; }
    TCommand Command { get; }
    CommandInfo CommandInfo { get; }
    Type CommandType { get; }
    Id CommandId { get; }
    HttpClientAdapterResponse? Response { get; set; }
}
