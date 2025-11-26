namespace Totem.Commands;

public interface IHttpCommandPipelineBuilder
{
    IHttpCommandPipelineBuilder Use<TMiddleware>(TMiddleware? middleware = default)
        where TMiddleware : IHttpCommandMiddleware;

    IHttpCommandPipeline Build();
}
