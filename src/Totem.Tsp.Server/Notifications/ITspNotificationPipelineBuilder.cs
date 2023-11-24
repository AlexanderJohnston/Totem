namespace Totem.Notifications;

public interface ITspNotificationPipelineBuilder
{
    ITspNotificationPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ITspNotificationMiddleware;

    ITspNotificationPipeline Build();
}
