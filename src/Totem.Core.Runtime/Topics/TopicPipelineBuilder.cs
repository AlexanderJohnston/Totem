namespace Totem.Topics;

public sealed class TopicPipelineBuilder : ITopicPipelineBuilder
{
    readonly List<ITopicMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public TopicPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public ITopicPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ITopicMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new TopicMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public ITopicPipeline Build() =>
        new TopicPipeline(_loggerFactory.CreateLogger<TopicPipeline>(), _steps, _map);
}
