namespace Totem.Hosting;

public static class WorkflowHostingExtensions
{
    public static ITotemBuilder AddWorkflows(this ITotemBuilder builder, Action<IWorkflowPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<WorkflowWhenMethodMiddleware>()
        .AddTransient<IWorkflowPipelineBuilder, WorkflowPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IWorkflowPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        foreach(var Workflow in builder.Services.GetRuntimeMap().Workflows)
        {
            builder.Services.AddTransient(Workflow.DeclaredType);
        }

        return builder;
    }

    public static IWorkflowPipelineBuilder Use(this IWorkflowPipelineBuilder builder, Func<IWorkflowContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new WorkflowMiddleware(middleware));

    public static IWorkflowPipelineBuilder Use(this IWorkflowPipelineBuilder builder, Func<IWorkflowContext<IEvent>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IWorkflowPipelineBuilder UseWhenMethod(this IWorkflowPipelineBuilder builder) =>
        builder.Use<WorkflowWhenMethodMiddleware>();
}
