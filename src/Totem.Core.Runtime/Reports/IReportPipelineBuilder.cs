namespace Totem.Reports;

public interface IReportPipelineBuilder
{
    IReportPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IReportMiddleware;

    IReportPipeline Build();
}
