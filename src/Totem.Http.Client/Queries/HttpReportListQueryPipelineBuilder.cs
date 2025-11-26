namespace Totem.Queries;

public sealed class HttpReportListQueryPipelineBuilder : IHttpReportListQueryPipelineBuilder
{
    readonly List<IHttpReportListQueryMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;

    public HttpReportListQueryPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public IHttpReportListQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IHttpReportListQueryMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new HttpReportListQueryMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IHttpReportListQueryPipeline Build() =>
        new HttpReportListQueryPipeline(_loggerFactory.CreateLogger<HttpReportListQueryPipeline>(), _steps);
}
