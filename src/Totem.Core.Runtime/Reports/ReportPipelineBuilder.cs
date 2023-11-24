namespace Totem.Reports;

public sealed class ReportPipelineBuilder : IReportPipelineBuilder
{
    readonly List<IReportMiddleware> _steps = new();
    readonly IServiceProvider _services;
    readonly ILoggerFactory _loggerFactory;
    readonly RuntimeMap _map;

    public ReportPipelineBuilder(IServiceProvider services, ILoggerFactory loggerFactory, RuntimeMap map)
    {
        _services = services;
        _loggerFactory = loggerFactory;
        _map = map;
    }

    public IReportPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IReportMiddleware
    {
        if(middleware is not null)
        {
            _steps.Add(middleware);
        }
        else
        {
            _steps.Add(new ReportMiddleware<TMiddleware>(_services));
        }

        return this;
    }

    public IReportPipeline Build() =>
        new ReportPipeline(_loggerFactory.CreateLogger<ReportPipeline>(), _steps, _map);
}
