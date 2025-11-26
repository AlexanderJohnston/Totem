namespace Totem.Queries;

public interface IHttpReportListQueryPipelineBuilder
{
    IHttpReportListQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IHttpReportListQueryMiddleware;

    IHttpReportListQueryPipeline Build();
}
