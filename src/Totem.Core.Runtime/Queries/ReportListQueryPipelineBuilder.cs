namespace Totem.Queries;

public sealed class ReportListQueryPipelineBuilder : IReportListQueryPipelineBuilder
{
    readonly List<IReportListQueryMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public ReportListQueryPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public IReportListQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IReportListQueryMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new ListQueryMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IReportListQueryPipeline Build() =>
        new ReportListQueryPipeline(_loggerFactory.CreateLogger<ReportListQueryPipeline>(), _steps, _map);
}
