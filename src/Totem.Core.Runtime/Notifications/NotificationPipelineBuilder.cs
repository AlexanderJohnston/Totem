namespace Totem.Notifications;

public sealed class NotificationPipelineBuilder : INotificationPipelineBuilder
{
    readonly List<INotificationMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public NotificationPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public INotificationPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : INotificationMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new NotificationMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public INotificationPipeline Build() =>
        new NotificationPipeline(_loggerFactory.CreateLogger<NotificationPipeline>(), _steps, _map);
}
