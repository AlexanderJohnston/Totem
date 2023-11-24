namespace Totem.Topics;

public interface ITopicTransaction
{
    ITopicContext<ICommand> Context { get; }
    ITopic Topic { get; }
    TimelinePosition Position { get; }

    Task CommitAsync();
    Task RollbackAsync();
}
