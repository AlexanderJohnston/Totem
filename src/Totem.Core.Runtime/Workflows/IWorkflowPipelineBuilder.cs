namespace Totem.Workflows;

public interface IWorkflowPipelineBuilder
{
    IWorkflowPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IWorkflowMiddleware;

    IWorkflowPipeline Build();
}
