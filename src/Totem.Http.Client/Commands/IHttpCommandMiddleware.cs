namespace Totem.Commands;

public interface IHttpCommandMiddleware
{
    Task InvokeAsync(IHttpCommandContext<IHttpCommand> context, Func<Task> next, CancellationToken cancellationToken);
}
