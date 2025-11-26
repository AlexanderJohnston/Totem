namespace Totem.Workflows;

public sealed class WorkflowPipelineBuilder : IWorkflowPipelineBuilder
{
    readonly List<IWorkflowMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public WorkflowPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public IWorkflowPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IWorkflowMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new WorkflowMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IWorkflowPipeline Build() =>
        new WorkflowPipeline(_loggerFactory.CreateLogger<WorkflowPipeline>(), _steps, _map);
}
