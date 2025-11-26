namespace Totem.Queries;

public interface IReportQueryPipelineBuilder
{
    IReportQueryPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IReportQueryMiddleware;

    IReportQueryPipeline Build();
}
