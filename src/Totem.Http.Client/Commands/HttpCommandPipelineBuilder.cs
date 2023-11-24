namespace Totem.Commands;

public sealed class HttpCommandPipelineBuilder : IHttpCommandPipelineBuilder
{
    readonly List<IHttpCommandMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;

    public HttpCommandPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public IHttpCommandPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IHttpCommandMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new HttpCommandMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IHttpCommandPipeline Build() =>
        new HttpCommandPipeline(_loggerFactory.CreateLogger<HttpCommandPipeline>(), _steps);
}
