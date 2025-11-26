namespace Totem.Subscriptions;

public sealed class TspSubscriptionPipelineBuilder : ITspSubscriptionPipelineBuilder
{
    readonly List<ITspSubscriptionMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;

    public TspSubscriptionPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public ITspSubscriptionPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : ITspSubscriptionMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new TspSubscriptionMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public ITspSubscriptionPipeline Build() =>
        new TspSubscriptionPipeline(_loggerFactory.CreateLogger<TspSubscriptionPipeline>(), _steps);
}
