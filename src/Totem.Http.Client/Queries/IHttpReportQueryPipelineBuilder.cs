namespace Totem.Queries;

public interface IHttpReportQueryPipelineBuilder
{
    IHttpReportQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IHttpReportQueryMiddleware;

    IHttpReportQueryPipeline Build();
}
