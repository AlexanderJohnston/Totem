namespace Totem.Queries;

public sealed class HttpReportQueryPipelineBuilder : IHttpReportQueryPipelineBuilder
{
    readonly List<IHttpReportQueryMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;

    public HttpReportQueryPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public IHttpReportQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IHttpReportQueryMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new HttpReportQueryMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IHttpReportQueryPipeline Build() =>
        new HttpReportQueryPipeline(_loggerFactory.CreateLogger<HttpReportQueryPipeline>(), _steps);
}
