namespace Totem;

public interface ICommandContext<out TCommand> : IMessageContext
    where TCommand : ICommand
{
    new CommandEnvelope Envelope { get; }
    TCommand Command { get; }
    CommandInfo CommandInfo { get; }
    CommandType CommandType { get; }
    Id CommandId { get; }
}
