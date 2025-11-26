namespace Totem;

public interface ITopicContext<out TCommand> : ICommandContext<TCommand>
    where TCommand : ICommand
{
    TimelineKey TopicKey { get; }
    TopicType TopicType { get; }
    Id TopicId { get; }
}
