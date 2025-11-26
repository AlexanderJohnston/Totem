namespace Totem.Hosting;

public static class TspNotificationHostingExtensions
{
    public static ITotemTspServerBuilder AddTspServerNotifications(this ITotemTspServerBuilder builder, Action<ITspNotificationPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<TspNotificationHubMiddleware>()
        .AddTransient<ITspNotificationPipelineBuilder, TspNotificationPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<ITspNotificationPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static ITotemTspServerBuilder AddTspReportNotifications(this ITotemTspServerBuilder builder)
    {
        builder.Services.AddSingleton<IReportWatcher, TspReportWatcher>();

        return builder;
    }

    public static ITspNotificationPipelineBuilder Use(this ITspNotificationPipelineBuilder builder, Func<ITspNotificationContext<ITspNotification>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new TspNotificationMiddleware(middleware));

    public static ITspNotificationPipelineBuilder Use(this ITspNotificationPipelineBuilder builder, Func<ITspNotificationContext<ITspNotification>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static ITspNotificationPipelineBuilder UseHub(this ITspNotificationPipelineBuilder builder) =>
        builder.Use<TspNotificationHubMiddleware>();
}
