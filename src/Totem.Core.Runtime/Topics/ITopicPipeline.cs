namespace Totem.Topics;

public interface ITopicPipeline
{
    Task<ITopicContext<ICommand>> RunAsync(ICommandContext<ICommand> commandContext, TopicRoute route, CancellationToken cancellationToken);
}
