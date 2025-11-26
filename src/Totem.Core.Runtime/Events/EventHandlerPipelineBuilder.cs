namespace Totem.Events;

public sealed class EventHandlerPipelineBuilder : IEventHandlerPipelineBuilder
{
    readonly List<IEventHandlerMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public EventHandlerPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public IEventHandlerPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IEventHandlerMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new EventHandlerMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IEventHandlerPipeline Build() =>
        new EventHandlerPipeline(_loggerFactory.CreateLogger<EventHandlerPipeline>(), _steps, _map);
}
