namespace Totem.Subscriptions;

public sealed class SubscriptionPipelineBuilder : ISubscriptionPipelineBuilder
{
    readonly List<ISubscriptionMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public SubscriptionPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public ISubscriptionPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ISubscriptionMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new SubscriptionMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public ISubscriptionPipeline Build() =>
        new SubscriptionPipeline(_loggerFactory.CreateLogger<SubscriptionPipeline>(), _steps, _map);
}
