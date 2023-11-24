namespace Totem.Queries;

public interface IReportListQueryPipelineBuilder
{
    IReportListQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IReportListQueryMiddleware;

    IReportListQueryPipeline Build();
}
