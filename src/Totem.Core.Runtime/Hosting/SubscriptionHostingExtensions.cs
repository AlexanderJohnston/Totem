namespace Totem.Hosting;

public static class SubscriptionHostingExtensions
{
    public static ITotemBuilder AddSubscriptions(this ITotemBuilder builder, Action<ISubscriptionPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<ISubscriptionBroker, SubscriptionBroker>()
        .AddSingleton<SubscriptionHandlerMiddleware>()
        .AddTransient<ISubscriptionPipelineBuilder, SubscriptionPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<ISubscriptionPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static ITotemBuilder AddSubscriptionHandlerServices(this ITotemBuilder builder)
    {
        foreach(var handler in builder.Services.GetRuntimeMap().SubscriptionHandlers)
        {
            builder.Services.AddTransient(handler.ServiceType, handler.DeclaredType);
        }

        return builder;
    }

    public static ISubscriptionPipelineBuilder Use(this ISubscriptionPipelineBuilder builder, Func<ISubscriptionContext<ISubscription>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new SubscriptionMiddleware(middleware));

    public static ISubscriptionPipelineBuilder Use(this ISubscriptionPipelineBuilder builder, Func<ISubscriptionContext<ISubscription>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static ISubscriptionPipelineBuilder UseHandler(this ISubscriptionPipelineBuilder builder) =>
        builder.Use<SubscriptionHandlerMiddleware>();
}
