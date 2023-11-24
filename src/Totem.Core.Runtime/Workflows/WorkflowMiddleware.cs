namespace Totem.Workflows;

public sealed class WorkflowMiddleware : IWorkflowMiddleware
{
    readonly Func<IWorkflowContext<IEvent>, Func<Task>, CancellationToken, Task> _middleware;

    public WorkflowMiddleware(Func<IWorkflowContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        _middleware = middleware;

    public Task InvokeAsync(IWorkflowContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _middleware(context, next, cancellationToken);
}

public sealed class WorkflowMiddleware<TService> : IWorkflowMiddleware
    where TService : IWorkflowMiddleware
{
    readonly IServiceProvider _services;

    public WorkflowMiddleware(IServiceProvider services) =>
        _services = services;

    public Task InvokeAsync(IWorkflowContext<IEvent> context, Func<Task> next, CancellationToken cancellationToken) =>
        _services.GetRequiredService<TService>().InvokeAsync(context, next, cancellationToken);
}
