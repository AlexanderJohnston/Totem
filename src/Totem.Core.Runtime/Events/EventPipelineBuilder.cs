namespace Totem.Events;

public sealed class EventPipelineBuilder : IEventPipelineBuilder
{
    readonly List<IEventMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public EventPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public IEventPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IEventMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new EventMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IEventPipeline Build() =>
        new EventPipeline(_loggerFactory.CreateLogger<EventPipeline>(), _steps, _map);
}
