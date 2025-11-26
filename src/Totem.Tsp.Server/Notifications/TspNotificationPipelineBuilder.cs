namespace Totem.Notifications;

public sealed class TspNotificationPipelineBuilder : ITspNotificationPipelineBuilder
{
    readonly List<ITspNotificationMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;

    public TspNotificationPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public ITspNotificationPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ITspNotificationMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new TspServerNotificationMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public ITspNotificationPipeline Build() =>
        new TspNotificationPipeline(_loggerFactory.CreateLogger<TspNotificationPipeline>(), _steps);
}
