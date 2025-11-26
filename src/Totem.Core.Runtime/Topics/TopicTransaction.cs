namespace Totem.Topics;

public sealed class TopicTransaction : ITopicTransaction
{
    readonly ITopicStore _store;
    readonly CancellationToken _cancellationToken;

    public TopicTransaction(
        ITopicStore store,
        ITopicContext<ICommand> context,
        ITopic topic,
        TimelinePosition position,
        CancellationToken cancellationToken)
    {
        _store = store;
        Context = context;
        Topic = topic;
        Position = position;
        _cancellationToken = cancellationToken;
    }

    public ITopicContext<ICommand> Context { get; }
    public ITopic Topic { get; }
    public TimelinePosition Position { get; }

    public async Task CommitAsync()
    {
        if(!_cancellationToken.IsCancellationRequested)
        {
            await _store.CommitAsync(this, _cancellationToken);
        }
    }

    public Task RollbackAsync() =>
        _store.RollbackAsync(this, CancellationToken.None);
}
