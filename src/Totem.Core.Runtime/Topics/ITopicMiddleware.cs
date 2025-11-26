namespace Totem.Topics;

public interface ITopicMiddleware
{
    Task InvokeAsync(ITopicContext<ICommand> context, Func<Task> next, CancellationToken cancellationToken);
}
