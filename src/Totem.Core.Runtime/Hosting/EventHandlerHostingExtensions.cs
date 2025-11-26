namespace Totem.Hosting;

public static class EventHandlerHostingExtensions
{
    public static ITotemBuilder AddEventHandlers(this ITotemBuilder builder, Action<IEventHandlerPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<EventHandlerHandleMiddleware>()
        .AddTransient<IEventHandlerPipelineBuilder, EventHandlerPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<IEventHandlerPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static ITotemBuilder AddEventHandlerServices(this ITotemBuilder builder)
    {
        foreach(var handler in builder.Services.GetRuntimeMap().EventHandlers)
        {
            builder.Services.AddTransient(handler.ServiceType, handler.DeclaredType);
        }

        return builder;
    }

    public static IEventHandlerPipelineBuilder Use(this IEventHandlerPipelineBuilder builder, Func<IEventHandlerContext<IEvent>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new EventHandlerMiddleware(middleware));

    public static IEventHandlerPipelineBuilder Use(this IEventHandlerPipelineBuilder builder, Func<IEventHandlerContext<IEvent>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static IEventHandlerPipelineBuilder UseHandler(this IEventHandlerPipelineBuilder builder) =>
        builder.Use<EventHandlerHandleMiddleware>();
}
