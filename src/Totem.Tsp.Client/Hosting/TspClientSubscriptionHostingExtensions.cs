namespace Totem.Hosting;

public static class TspClientSubscriptionHostingExtensions
{
    public static ITotemTspClientBuilder AddTspSubscriptions(this ITotemTspClientBuilder builder, Action<ITspSubscriptionPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<TspSubscriptionHubMiddleware>()
        .AddTransient<ITspSubscriptionPipelineBuilder, TspSubscriptionPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<ITspSubscriptionPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static ITspSubscriptionPipelineBuilder Use(this ITspSubscriptionPipelineBuilder builder, Func<ITspSubscriptionContext<ITspSubscription>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new TspSubscriptionMiddleware(middleware));

    public static ITspSubscriptionPipelineBuilder Use(this ITspSubscriptionPipelineBuilder builder, Func<ITspSubscriptionContext<ITspSubscription>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static ITspSubscriptionPipelineBuilder UseHub(this ITspSubscriptionPipelineBuilder builder) =>
        builder.Use<TspSubscriptionHubMiddleware>();
}
