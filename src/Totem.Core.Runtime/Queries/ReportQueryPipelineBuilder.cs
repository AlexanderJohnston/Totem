namespace Totem.Queries;

public sealed class ReportQueryPipelineBuilder : IReportQueryPipelineBuilder
{
    readonly List<IReportQueryMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public ReportQueryPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public IReportQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IReportQueryMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new RowQueryMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IReportQueryPipeline Build() =>
        new ReportQueryPipeline(_loggerFactory.CreateLogger<ReportQueryPipeline>(), _steps, _map);
}
