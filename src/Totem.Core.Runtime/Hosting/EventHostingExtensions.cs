namespace Totem.Hosting;

public static class EventHostingExtensions
{
    public static ITotemBuilder AddEvents(this ITotemBuilder builder, Action<IEventPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<ReportBusMiddleware>()
        .AddSingleton<WorkflowBusMiddleware>()
        .AddSingleton<HandlerBusMiddleware>()
        .AddTransient<IEventPipelineBuilder, EventPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IEventPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static IEventPipelineBuilder Use(this IEventPipelineBuilder builder, Func<IEventContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new EventMiddleware(middleware));

    public static IEventPipelineBuilder Use(this IEventPipelineBuilder builder, Func<IEventContext<IEvent>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IEventPipelineBuilder UseReportBus(this IEventPipelineBuilder builder) =>
        builder.Use<ReportBusMiddleware>();

    public static IEventPipelineBuilder UseWorkflowBus(this IEventPipelineBuilder builder) =>
        builder.Use<WorkflowBusMiddleware>();

    public static IEventPipelineBuilder UseHandlerBus(this IEventPipelineBuilder builder) =>
        builder.Use<HandlerBusMiddleware>();
}
