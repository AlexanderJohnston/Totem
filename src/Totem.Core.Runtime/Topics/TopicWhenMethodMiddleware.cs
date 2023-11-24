namespace Totem.Topics;

public sealed class TopicWhenMethodMiddleware : ITopicMiddleware
{
    readonly ITopicStore _store;

    public TopicWhenMethodMiddleware(ITopicStore store) =>
        _store = store;

    public async Task InvokeAsync(ITopicContext<ICommand> context, Func<Task> next, CancellationToken cancellationToken)
    {
        var transaction = await _store.StartTransactionAsync(context, cancellationToken);
        var topic = transaction.Topic;
        var when = context.CommandType.When;

        try
        {
            await when.CallAsync(topic, context, cancellationToken);

            context.ExpectNoErrors();
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }

        await transaction.CommitAsync();
        await next();
    }
}
