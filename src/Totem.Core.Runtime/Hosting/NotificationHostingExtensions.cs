namespace Totem.Hosting;

public static class NotificationHostingExtensions
{
    public static ITotemBuilder AddNotifications(this ITotemBuilder builder, Action<INotificationPipelineBuilder> declarePipeline)
    {
        builder.Services
        .AddSingleton<NotificationHandlerMiddleware>()
        .AddTransient<INotificationPipelineBuilder, NotificationPipelineBuilder>()
        .AddSingleton(provider =>
        {
            var pipelineBuilder = provider.GetRequiredService<INotificationPipelineBuilder>();

            declarePipeline(pipelineBuilder);

            return pipelineBuilder.Build();
        });

        return builder;
    }

    public static ITotemBuilder AddNotificationHandlerServices(this ITotemBuilder builder)
    {
        foreach(var handler in builder.Services.GetRuntimeMap().NotificationHandlers)
        {
            builder.Services.AddTransient(handler.ServiceType, handler.DeclaredType);
        }

        return builder;
    }

    public static INotificationPipelineBuilder Use(this INotificationPipelineBuilder builder, Func<INotificationContext<INotification>, Func<Task>, CancellationToken, Task> middleware) =>
        builder.Use(new NotificationMiddleware(middleware));

    public static INotificationPipelineBuilder Use(this INotificationPipelineBuilder builder, Func<INotificationContext<INotification>, Func<Task>, Task> middleware) =>
        builder.Use((context, next, _) => middleware(context, next));

    public static INotificationPipelineBuilder UseHandler(this INotificationPipelineBuilder builder) =>
        builder.Use<NotificationHandlerMiddleware>();
}
