namespace Totem.Notifications;

public interface INotificationPipelineBuilder
{
    INotificationPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : INotificationMiddleware;

    INotificationPipeline Build();
}
